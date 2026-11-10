using System.Collections.Generic;
using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 世界级子物体仓库：持有全部套路、同目标记忆池、目标快照与命中缓冲，是生成、回收子物体的唯一入口。
    /// 两种形态：纯批处理记录（SpawnRoutine，无实体）与实体子物体（SpawnEntity，可带 Logic，也可同时带关联记录）。
    /// 由 SysSubobject 创建并按帧驱动；套路步进期间处于锁定状态，此时的生成请求会排队到本帧末尾。
    /// </summary>
    public class UniSubobjectsComponent : MetaComponent
    {
        // 视为零向量的平方长度
        private const float DirectionSqrEpsilon = 1e-10f;
        // 句柄失效时 GetMotion 返回的占位，每次返回前清零
        private static SubobjectMotion s_deadMotion;

        private readonly List<SubobjectRoutine> _routines = new();         // 下标即 SubobjectHandle.Routine；0 为默认套路
        private readonly List<SubobjectSpawnArgs> _pendingSpawns = new(); // 锁定期间请求的批处理生成，回收阶段后落地
        private readonly List<SubobjectHitEvent> _hits = new();           // 本帧命中，派发后清空
        private bool _locked;                                             // 套路步进期间为 true

        public LogicWorld LogicWorld { get; private set; }
        public MetaWorld MetaWorld { get; private set; }
        public SubobjectHitMemoryPool HitMemory { get; } = new();
        public SubobjectTargetSnapshot Targets { get; } = new();
        public float WorldTime { get; private set; }         // 秒，按 SysSubobject 收到的 dt 累加；命中间隔与同目标记忆都以它为时钟
        public ISubobjectHitSink HitSink { get; set; }       // 命中结算出口；为空时命中只计次
        public ISubobjectViewSink ViewSink { get; set; }     // 批处理记录的表现出口；为空时不生成表现
        public string DefaultLogicContainer { get; set; }    // SubobjectEntityOptions.LogicContainer 为空时使用的逻辑配置容器名
        public int RoutineCount => _routines.Count;

        //////////////////////////////////////////////////////////////////////////
        /// MetaComponent：override

        public override void DisposeOnRemove()
        {
            for (var i = 0; i < _routines.Count; i++)
            {
                _routines[i].RemoveAll();
                _routines[i].Detach();
            }
            _routines.Clear();
            _pendingSpawns.Clear();
            _hits.Clear();
            HitMemory.Clear();
            Targets.Clear();
            _locked = false;
            LogicWorld = null;
            MetaWorld = null;
            WorldTime = 0f;
            HitSink = null;
            ViewSink = null;
            DefaultLogicContainer = null;
            base.DisposeOnRemove();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：套路

        /// <summary>
        /// 注册一个套路实例并返回其序号，供 SubobjectSpawnArgs.Routine 使用。同一实例只能注册一次。
        /// </summary>
        public short RegisterRoutine(SubobjectRoutine routine)
        {
            if (routine == null)
            {
                G.LogError("RegisterRoutine routine == null");
                return -1;
            }
            if (routine.RoutineIndex >= 0)
            {
                G.LogError($"RegisterRoutine {routine.GetType().Name} 已注册，序号 {routine.RoutineIndex}");
                return routine.RoutineIndex;
            }
            if (_locked)
            {
                G.LogError("RegisterRoutine 不能在套路步进期间调用");
                return -1;
            }

            var index = (short)_routines.Count;
            routine.Attach(this, index);
            _routines.Add(routine);
            return index;
        }

        /// <summary>
        /// 按序号取套路；序号越界时返回 null。
        /// </summary>
        public SubobjectRoutine GetRoutine(short routineIndex)
        {
            if (routineIndex < 0 || routineIndex >= _routines.Count)
                return null;
            return _routines[routineIndex];
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：生成与回收

        /// <summary>
        /// 生成一条纯批处理记录。套路步进期间调用时排队到本帧回收之后落地，此时返回无效句柄。
        /// </summary>
        public SubobjectHandle SpawnRoutine(in SubobjectSpawnArgs args)
        {
            var routine = GetRoutine(args.Routine);
            if (routine == null)
            {
                G.LogError($"SpawnRoutine 套路 {args.Routine} 未注册");
                return default;
            }
            if (_locked)
            {
                _pendingSpawns.Add(args);
                return default;
            }
            return routine.Add(in args, null);
        }

        /// <summary>
        /// 生成实体子物体：挂变换、阵营、玩家归属与 SubobjectComponent；按 options 创建 Logic、关联记录。
        /// 带关联记录时由记录驱动位置并计时，否则 Motion.LifeLeft 为正时转为 ComLife。
        /// 套路步进期间不可调用，返回 null。
        /// </summary>
        public LogicEntity SpawnEntity(in SubobjectSpawnArgs args, in SubobjectEntityOptions options)
        {
            if (_locked)
            {
                G.LogError("SpawnEntity 不能在套路步进期间调用");
                return null;
            }

            SubobjectRoutine routine = null;
            if (options.WithRoutine)
            {
                routine = GetRoutine(args.Routine);
                if (routine == null)
                {
                    G.LogError($"SpawnEntity 套路 {args.Routine} 未注册");
                    return null;
                }
            }

            var entity = LogicWorld.CreateEntity();
            entity.SetComTransform(args.Motion.Position, Quaternion.identity, Vector3.one);
            FaceTowards(entity, args.Motion.Direction);
            if (args.Source.Faction != EFaction.Invalid)
                entity.Faction = args.Source.Faction;
            if (args.Origin.Player != null)
                entity.AddComOwnerPlayer(args.Origin.Player);
            entity.AddComSubobject(in args.Source, in args.Origin);

            var component = entity.comSubobject;
            if (routine != null)
                component.LinkRoutine(routine.Add(in args, entity));
            if (options.LogicConfigId != 0)
                component.SetLogic(CreateLogic(entity, in args, in options));
            if (routine == null && args.Motion.LifeLeft > 0f)
                entity.AddComLife(args.Motion.LifeLeft);
            return entity;
        }

        /// <summary>
        /// 句柄指向未到期的记录时返回 true。
        /// </summary>
        public bool IsAlive(SubobjectHandle handle)
        {
            var routine = GetRoutine(handle.Routine);
            return routine != null && routine.IsAlive(handle);
        }

        /// <summary>
        /// 解析句柄，成功时给出所属套路与当前稠密下标；下标只在当前阶段内有效。
        /// </summary>
        public bool TryResolve(SubobjectHandle handle, out SubobjectRoutine routine, out int dense)
        {
            routine = GetRoutine(handle.Routine);
            dense = -1;
            return routine != null && routine.TryGetDense(handle, out dense);
        }

        /// <summary>
        /// 取记录的运动数据用于就地修改；句柄失效时报错并返回一个丢弃用的占位。
        /// </summary>
        public ref SubobjectMotion GetMotion(SubobjectHandle handle)
        {
            if (TryResolve(handle, out var routine, out var dense))
                return ref routine.MotionAt(dense);

            G.LogError($"GetMotion 句柄已失效 {handle}");
            s_deadMotion = default;
            return ref s_deadMotion;
        }

        /// <summary>
        /// 回收记录，返回是否找到。关联实体只断开关联，不随之销毁。
        /// 套路步进期间调用时只标记到期，本帧回收阶段统一处理。
        /// </summary>
        public bool Despawn(SubobjectHandle handle)
        {
            if (!TryResolve(handle, out var routine, out var dense))
                return false;

            routine.DetachLinked(dense, false);
            if (_locked)
                routine.MarkExpired(dense);
            else
                routine.RemoveDense(dense);
            return true;
        }

        // SysSubobject 在组件移除通知中调用：回收旧组件的关联记录，不再访问实体
        internal void ReleaseLinkedRecord(SubobjectHandle handle)
        {
            if (!TryResolve(handle, out var routine, out var dense))
                return;

            routine.UnlinkDense(dense);
            if (_locked)
                routine.MarkExpired(dense);
            else
                routine.RemoveDense(dense);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：由 SysSubobject 按帧驱动

        internal void Init(LogicWorld logicWorld, MetaWorld metaWorld)
        {
            LogicWorld = logicWorld;
            MetaWorld = metaWorld;
            WorldTime = 0f;
            RegisterRoutine(new SubobjectRoutine());
        }

        internal void AdvanceTime(float dt)
        {
            WorldTime += dt;
        }

        internal void RebuildTargets(List<LogicEntity> targets)
        {
            Targets.Rebuild(targets);
        }

        // 各套路积分并检测命中；期间锁定，生成请求排队
        internal void StepRoutines(float dt)
        {
            _locked = true;
            try
            {
                for (var i = 0; i < _routines.Count; i++)
                    _routines[i].Step(dt, WorldTime, Targets, _hits);
            }
            finally
            {
                _locked = false;
            }
        }

        // 先交给结算出口，再通知关联实体的 Logic
        internal void DispatchHits()
        {
            if (_hits.Count == 0)
                return;

            HitSink?.OnSubobjectHits(this, _hits);
            for (var i = 0; i < _hits.Count; i++)
            {
                var linked = _hits[i].Linked;
                if (linked == null || !linked.isEnabled || !linked.hasComSubobject)
                    continue;
                linked.comSubobject.Logic?.OnRoutineHit(_hits[i]);
            }
            _hits.Clear();
        }

        internal void SyncLinkedEntities()
        {
            for (var i = 0; i < _routines.Count; i++)
                _routines[i].SyncLinked();
        }

        internal void SyncViews()
        {
            if (ViewSink == null)
                return;
            for (var i = 0; i < _routines.Count; i++)
            {
                if (_routines[i].Count > 0)
                    ViewSink.SyncRoutine(_routines[i]);
            }
        }

        internal void RecycleExpired()
        {
            for (var i = 0; i < _routines.Count; i++)
                _routines[i].RecycleExpired();
        }

        internal void FlushPendingSpawns()
        {
            for (var i = 0; i < _pendingSpawns.Count; i++)
            {
                var args = _pendingSpawns[i];
                GetRoutine(args.Routine)?.Add(in args, null);
            }
            _pendingSpawns.Clear();
        }

        // 用托管旋转把实体朝向对准 direction；direction 为零向量时不改
        private static void FaceTowards(LogicEntity entity, Vector3 direction)
        {
            if (direction.sqrMagnitude < DirectionSqrEpsilon)
                return;

            var transform = entity.comTransform;
            var face = transform.WorldFaceDir;
            if (face.sqrMagnitude < DirectionSqrEpsilon)
                return;
            transform.SetRotation(SubobjectMath.FromToRotation(face.normalized, direction.normalized) * transform.rotation);
        }

        // 创建失败时回收 GenInfo 并返回 null
        private SubobjectLogic CreateLogic(LogicEntity entity, in SubobjectSpawnArgs args, in SubobjectEntityOptions options)
        {
            var svc = G.CustomLogic;
            var genInfo = SubobjectGenInfo.New(svc, MetaWorld, entity, in args.Source, in args.Origin);
            genInfo.LogicConfigID = options.LogicConfigId;
            genInfo.ConfigContainerName = string.IsNullOrEmpty(options.LogicContainer) ? DefaultLogicContainer : options.LogicContainer;

            var logic = svc.CreateLogic<SubobjectLogic>(genInfo);
            if (logic == null)
                svc.DestroyGenInfo(genInfo);
            return logic;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class MetaWorld
    {
        public UniSubobjectsComponent comUniSubobjects
        {
            get { return GetUniqueComponent<UniSubobjectsComponent>(MetaComponentsLookup.ComUniSubobjects); }
        }

        public bool hasComUniSubobjects
        {
            get { return HasUniqueComponent(MetaComponentsLookup.ComUniSubobjects); }
        }

        /// <summary>
        /// 创建子物体仓库并注册默认套路；已存在时先卸载旧仓库。
        /// </summary>
        public void SetComUniSubobjects(LogicWorld logicWorld)
        {
            var index = MetaComponentsLookup.ComUniSubobjects;
            var unique = UniqueEntity;
            if (unique.HasComponent(index))
                unique.RemoveComponent(index);
            var component = (UniSubobjectsComponent)unique.CreateComponent(index, typeof(UniSubobjectsComponent));
            component.Init(logicWorld, this);
            SetUniqueComponent(index, component);
        }

        public void RemoveComUniSubobjects()
        {
            if (hasComUniSubobjects)
            {
                UniqueEntity.RemoveComponent(MetaComponentsLookup.ComUniSubobjects);
            }
        }
    }

    public static partial class MetaComponentsLookup
    {
        private static ComponentTypeIndex _ComUniSubobjectsIndex = new(typeof(UniSubobjectsComponent));
        public static int ComUniSubobjects => _ComUniSubobjectsIndex.Index;
    }
}
