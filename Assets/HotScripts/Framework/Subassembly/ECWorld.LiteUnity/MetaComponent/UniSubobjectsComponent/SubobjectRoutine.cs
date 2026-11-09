using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 一类固定套路：持有名下全部子物体记录的稠密数组并整批处理。默认实现提供静止、直线、追踪运动与通用命中规则。
    /// 新的固定套路继承本类并注册到 UniSubobjectsComponent：重写 IntegrateAll 改运动，重写 AcceptHit 改单目标判定；
    /// 子类自己的逐条数据放在并行数组里，并在 OnCapacityChanged / OnDenseAdded / OnDenseMoved / OnDenseFreed 中维护。
    /// 稠密下标会因回收而变化，只在同一阶段内有效；跨帧持有请用 SubobjectHandle。
    /// 以后接 Job：热数据与规则都是值类型数组，可整体换成 NativeArray；积分与粗筛移到 AOT 程序集用 Burst 编译
    /// （HybridCLR 热更代码不能用 Burst），AcceptHit 与派发留在主线程，是否 Job 化按套路逐个决定。
    /// </summary>
    public class SubobjectRoutine
    {
        // 首次分配的记录容量，不足时倍增
        private const int InitialCapacity = 64;
        // 视为零向量的平方长度
        private const float DirectionSqrEpsilon = 1e-10f;
        // 朝向与运动方向的点积高于此值时不再改写关联实体的旋转
        private const float FaceAlignedDot = 0.99999f;

        // 稠密数组，[0, _count) 有效；回收时把末尾记录换到空位
        protected SubobjectMotion[] _motions = new SubobjectMotion[InitialCapacity];
        protected SubobjectHitPolicy[] _policies = new SubobjectHitPolicy[InitialCapacity];
        protected SubobjectHitState[] _states = new SubobjectHitState[InitialCapacity];
        protected SubobjectSource[] _sources = new SubobjectSource[InitialCapacity]; // 冷数据，只在命中与派发时读
        protected int[] _viewKeys = new int[InitialCapacity];                         // 关联实体的记录恒为 0，表现走实体自身
        // 主线程托管并行数组
        protected SubobjectOrigin[] _origins = new SubobjectOrigin[InitialCapacity];
        protected LogicEntity[] _linked = new LogicEntity[InitialCapacity]; // 关联实体；实体卸载 SubobjectComponent 或记录回收时清空
        protected int _count;

        // 句柄间接表，与稠密数组同容量
        private int[] _denseToSlot = new int[InitialCapacity];
        private int[] _slotToDense = new int[InitialCapacity]; // -1 表示空闲槽
        private int[] _generations = new int[InitialCapacity];
        private int[] _freeSlots = new int[InitialCapacity];   // 空闲槽栈
        private int _freeSlotCount;
        private int _slotHighWater; // 从未使用过的首个槽位

        public short RoutineIndex { get; private set; } = -1; // 注册到 UniSubobjectsComponent 时分配
        public int Count => _count;                           // 当前记录数，含已到期待回收的记录
        protected UniSubobjectsComponent Owner { get; private set; }

        //////////////////////////////////////////////////////////////////////////
        /// This：可重写的批处理步骤

        /// <summary>
        /// 整批积分。调用前 PrevPosition 已写为本帧起点；默认实现按 Kind 处理静止、直线与追踪，跳过已到期记录。
        /// 重写时不得生成或回收记录。
        /// </summary>
        public virtual void IntegrateAll(float dt, SubobjectTargetSnapshot targets)
        {
            for (var i = 0; i < _count; i++)
            {
                if (_states[i].Expired)
                    continue;

                ref var motion = ref _motions[i];
                if (motion.Kind == ESubobjectMotionKind.None)
                    continue;
                if (motion.Kind == ESubobjectMotionKind.Homing)
                    Steer(ref motion, dt, targets);
                motion.Position += motion.Direction * (motion.Speed * dt);
            }
        }

        /// <summary>
        /// 单目标判定：几何、层、阵营、来源过滤与全局间隔、总次数都已通过后调用，返回 false 表示放过这个目标。
        /// 默认实现按 PerTargetMax 与 PerTargetInterval 查询并写入同目标记忆。重写时不得生成或回收记录。
        /// </summary>
        protected virtual bool AcceptHit(int dense, int targetCreationIndex, float now)
        {
            ref readonly var policy = ref _policies[dense];
            if (policy.PerTargetMax <= 0 && policy.PerTargetInterval <= 0f)
                return true;

            var memory = Owner.HitMemory;
            ref var state = ref _states[dense];
            var entryIndex = memory.Find(state.MemoryHead, targetCreationIndex);
            if (entryIndex < 0)
            {
                state.MemoryHead = memory.Push(state.MemoryHead, targetCreationIndex, now + policy.PerTargetInterval);
                return true;
            }

            ref var entry = ref memory.At(entryIndex);
            if (policy.PerTargetMax > 0 && entry.Count >= policy.PerTargetMax)
                return false;
            if (now < entry.NextTime)
                return false;

            entry.Count++;
            entry.NextTime = now + policy.PerTargetInterval;
            return true;
        }

        // 稠密数组扩容后调用，子类按 capacity 扩自己的并行数组
        protected virtual void OnCapacityChanged(int capacity)
        {
        }

        // 新记录写入 dense 后调用
        protected virtual void OnDenseAdded(int dense)
        {
        }

        // 回收时末尾记录从 from 搬到 to 后调用
        protected virtual void OnDenseMoved(int from, int to)
        {
        }

        // 记录即将被回收时调用，此时 dense 处数据仍完整
        protected virtual void OnDenseFreed(int dense)
        {
        }

        // 追踪转向：目标不在本帧快照中时放弃追踪并保持当前方向
        protected static void Steer(ref SubobjectMotion motion, float dt, SubobjectTargetSnapshot targets)
        {
            if (motion.HomingTarget == 0)
                return;
            if (!targets.TryGetPosition(motion.HomingTarget, out var targetPosition))
            {
                motion.HomingTarget = 0;
                return;
            }

            var toTarget = targetPosition - motion.Position;
            var distanceSqr = toTarget.sqrMagnitude;
            if (distanceSqr < DirectionSqrEpsilon)
                return;

            var desired = toTarget / Mathf.Sqrt(distanceSqr);
            if (motion.TurnRate <= 0f || motion.Direction.sqrMagnitude < DirectionSqrEpsilon)
            {
                motion.Direction = desired;
                return;
            }
            motion.Direction = SubobjectMath.RotateTowards(motion.Direction, desired, motion.TurnRate * dt);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：查询

        /// <summary>
        /// 句柄仍指向本套路中的记录（含已到期待回收的记录）时返回 true，并给出当前稠密下标。
        /// </summary>
        public bool TryGetDense(SubobjectHandle handle, out int dense)
        {
            dense = -1;
            if (handle.Routine != RoutineIndex || handle.Generation == 0)
                return false;

            var slot = handle.Slot;
            if ((uint)slot >= (uint)_slotHighWater || _generations[slot] != handle.Generation)
                return false;

            dense = _slotToDense[slot];
            return dense >= 0;
        }

        /// <summary>
        /// 句柄指向未到期的记录时返回 true。
        /// </summary>
        public bool IsAlive(SubobjectHandle handle)
        {
            return TryGetDense(handle, out var dense) && !_states[dense].Expired;
        }

        /// <summary>
        /// 取稠密下标处记录的句柄。
        /// </summary>
        public SubobjectHandle HandleAt(int dense)
        {
            var slot = _denseToSlot[dense];
            return new SubobjectHandle { Routine = RoutineIndex, Slot = slot, Generation = _generations[slot] };
        }

        /// <summary>
        /// 运动数据，可就地修改方向、速度、追踪目标等。
        /// </summary>
        public ref SubobjectMotion MotionAt(int dense) => ref _motions[dense];

        /// <summary>
        /// 命中规则。
        /// </summary>
        public ref readonly SubobjectHitPolicy PolicyAt(int dense) => ref _policies[dense];

        /// <summary>
        /// 命中状态。
        /// </summary>
        public ref readonly SubobjectHitState StateAt(int dense) => ref _states[dense];

        /// <summary>
        /// 来源快照。
        /// </summary>
        public ref readonly SubobjectSource SourceAt(int dense) => ref _sources[dense];

        /// <summary>
        /// 来源的主线程侧引用。
        /// </summary>
        public ref readonly SubobjectOrigin OriginAt(int dense) => ref _origins[dense];

        /// <summary>
        /// 表现键；0 表示不交给 ISubobjectViewSink。
        /// </summary>
        public int ViewKeyAt(int dense) => _viewKeys[dense];

        /// <summary>
        /// 关联实体；纯批处理记录为 null。
        /// </summary>
        public LogicEntity LinkedAt(int dense) => _linked[dense];

        //////////////////////////////////////////////////////////////////////////
        /// This：由 UniSubobjectsComponent 驱动

        internal void Attach(UniSubobjectsComponent owner, short routineIndex)
        {
            Owner = owner;
            RoutineIndex = routineIndex;
        }

        // 须在 RemoveAll 之后调用；保留槽位代数，避免旧句柄在重新注册后误命中
        internal void Detach()
        {
            Owner = null;
            RoutineIndex = -1;
        }

        // 写入一条新记录；linked 非空时为共存的实体子物体，表现走实体自身
        internal SubobjectHandle Add(in SubobjectSpawnArgs args, LogicEntity linked)
        {
            EnsureCapacity(_count + 1);
            var slot = AllocSlot();
            var dense = _count++;
            _slotToDense[slot] = dense;
            _denseToSlot[dense] = slot;

            var motion = args.Motion;
            motion.PrevPosition = motion.Position;
            if (motion.Direction.sqrMagnitude > DirectionSqrEpsilon)
                motion.Direction.Normalize();
            if (motion.LifeLeft <= 0f)
                motion.LifeLeft = float.PositiveInfinity;

            _motions[dense] = motion;
            _policies[dense] = args.Policy;
            _states[dense] = new SubobjectHitState { MemoryHead = -1 };
            _sources[dense] = args.Source;
            _origins[dense] = args.Origin;
            _viewKeys[dense] = linked == null ? args.ViewKey : 0;
            _linked[dense] = linked;
            OnDenseAdded(dense);

            var handle = HandleAt(dense);
            if (_viewKeys[dense] != 0)
                Owner.ViewSink?.OnSpawn(handle, _viewKeys[dense], in _motions[dense]);
            return handle;
        }

        // 一帧的批处理：写起点、整批积分、逐条命中检测、扣寿命。期间 UniSubobjectsComponent 处于锁定状态
        internal void Step(float dt, float now, SubobjectTargetSnapshot targets, List<SubobjectHitEvent> hits)
        {
            for (var i = 0; i < _count; i++)
                _motions[i].PrevPosition = _motions[i].Position;

            IntegrateAll(dt, targets);

            var canHit = targets.Count > 0;
            for (var i = 0; i < _count; i++)
            {
                if (_states[i].Expired)
                    continue;
                if (canHit)
                    CollideOne(i, now, targets, hits);

                ref var state = ref _states[i];
                if (state.Expired)
                    continue;
                ref var motion = ref _motions[i];
                motion.LifeLeft -= dt;
                if (motion.LifeLeft <= 0f)
                    state.Expired = true;
            }
        }

        // 把运动结果写回关联实体的变换；实体数量少，走带替换事件的普通写入。
        // 关联实体已进入死亡流程时记录随之到期，不再继续命中
        internal void SyncLinked()
        {
            for (var i = 0; i < _count; i++)
            {
                var entity = _linked[i];
                if (entity == null)
                    continue;
                if (!entity.isEnabled || entity.hasComDeath)
                {
                    _states[i].Expired = true;
                    continue;
                }

                ref readonly var motion = ref _motions[i];
                entity.SetPosition(motion.Position);
                if (motion.Kind == ESubobjectMotionKind.None || motion.Direction.sqrMagnitude < DirectionSqrEpsilon)
                    continue;

                var transform = entity.comTransform;
                var face = transform.WorldFaceDir;
                if (face.sqrMagnitude < DirectionSqrEpsilon || Vector3.Dot(face.normalized, motion.Direction) > FaceAlignedDot)
                    continue;
                transform.SetRotation(SubobjectMath.FromToRotation(face.normalized, motion.Direction) * transform.rotation);
            }
        }

        // 回收全部到期记录；关联实体随之挂死亡组件
        internal void RecycleExpired()
        {
            for (var i = _count - 1; i >= 0; i--)
            {
                if (!_states[i].Expired)
                    continue;
                DetachLinked(i, true);
                RemoveDense(i);
            }
        }

        // 回收全部记录，不处理关联实体的死亡；世界销毁时使用
        internal void RemoveAll()
        {
            for (var i = _count - 1; i >= 0; i--)
            {
                DetachLinked(i, false);
                RemoveDense(i);
            }
        }

        // 标记到期，交给本帧回收阶段处理
        internal void MarkExpired(int dense)
        {
            _states[dense].Expired = true;
        }

        // 只断开数组侧的关联，不访问实体；供实体卸载组件时使用
        internal void UnlinkDense(int dense)
        {
            _linked[dense] = null;
        }

        // 断开关联实体并清掉实体组件上的句柄；killLinked 为 true 时给仍存活的实体挂死亡组件
        internal void DetachLinked(int dense, bool killLinked)
        {
            var entity = _linked[dense];
            if (entity == null)
                return;

            _linked[dense] = null;
            if (!entity.isEnabled)
                return;

            if (entity.hasComSubobject)
                entity.comSubobject.ClearRoutineLink(HandleAt(dense));
            if (killLinked && !entity.hasComDeath)
                entity.AddComDeath(null);
        }

        // 立即回收一条记录：归还命中记忆、通知表现，再把末尾记录换到空位
        internal void RemoveDense(int dense)
        {
            var slot = _denseToSlot[dense];
            var handle = HandleAt(dense);
            Owner.HitMemory.FreeChain(_states[dense].MemoryHead);
            if (_viewKeys[dense] != 0)
                Owner.ViewSink?.OnDespawn(handle);
            OnDenseFreed(dense);

            var last = _count - 1;
            if (dense != last)
            {
                _motions[dense] = _motions[last];
                _policies[dense] = _policies[last];
                _states[dense] = _states[last];
                _sources[dense] = _sources[last];
                _viewKeys[dense] = _viewKeys[last];
                _origins[dense] = _origins[last];
                _linked[dense] = _linked[last];
                var movedSlot = _denseToSlot[last];
                _denseToSlot[dense] = movedSlot;
                _slotToDense[movedSlot] = dense;
                OnDenseMoved(last, dense);
            }

            _origins[last] = default;
            _linked[last] = null;
            _count = last;

            _slotToDense[slot] = -1;
            _generations[slot] = _generations[slot] == int.MaxValue ? 1 : _generations[slot] + 1;
            _freeSlots[_freeSlotCount++] = slot;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：内部

        // 单条记录的命中检测：先过全局闸门，再沿线段按先后逐个判定候选目标
        private void CollideOne(int dense, float now, SubobjectTargetSnapshot targets, List<SubobjectHitEvent> hits)
        {
            ref readonly var policy = ref _policies[dense];
            if (policy.TargetLayers == 0 || !IsGateOpen(dense, now))
                return;

            ref readonly var motion = ref _motions[dense];
            var from = motion.PrevPosition;
            var to = motion.Position;
            var found = targets.Query(from, to, policy.Radius, policy.TargetLayers);
            for (var n = 0; n < found; n++)
            {
                var target = targets.ResultIndex(n);
                if (!PassesSourceAndFaction(dense, targets, target))
                    continue;
                if (!AcceptHit(dense, targets.CreationIndexAt(target), now))
                    continue;

                ref var state = ref _states[dense];
                state.HitCount++;
                if (policy.HitInterval > 0f)
                    state.NextHitTime = now + policy.HitInterval;
                hits.Add(BuildEvent(dense, targets, target, from + (to - from) * targets.ResultParam(n)));

                if (policy.MaxHitCount > 0 && state.HitCount >= policy.MaxHitCount)
                {
                    if (policy.DestroyWhenExhausted)
                        state.Expired = true;
                    return;
                }
                if (now < state.NextHitTime)
                    return;
            }
        }

        // 总次数未用尽且已过全局间隔
        private bool IsGateOpen(int dense, float now)
        {
            ref readonly var policy = ref _policies[dense];
            ref readonly var state = ref _states[dense];
            if (policy.MaxHitCount > 0 && state.HitCount >= policy.MaxHitCount)
                return false;
            return now >= state.NextHitTime;
        }

        // 关联实体不命中自己；IgnoreSource 时跳过 Root 与 Parent；阵营关系须在 Factions 之内
        private bool PassesSourceAndFaction(int dense, SubobjectTargetSnapshot targets, int target)
        {
            var targetCreation = targets.CreationIndexAt(target);
            var linked = _linked[dense];
            if (linked != null && linked.creationIndex == targetCreation)
                return false;

            ref readonly var policy = ref _policies[dense];
            ref readonly var source = ref _sources[dense];
            if (policy.IgnoreSource && (targetCreation == source.RootCreationIndex || targetCreation == source.ParentCreationIndex))
                return false;

            var relation = SubobjectMath.Relation(source.Faction, targets.FactionAt(target));
            return (policy.Factions & relation) != 0;
        }

        private SubobjectHitEvent BuildEvent(int dense, SubobjectTargetSnapshot targets, int target, Vector3 point)
        {
            var targetEntity = targets.EntityAt(target);
            return new SubobjectHitEvent
            {
                Handle = HandleAt(dense),
                Target = SubobjectEntityRef.Of(targetEntity),
                TargetEntityId = targetEntity.ID,
                Point = point,
                EffectId = _policies[dense].EffectId,
                Source = _sources[dense],
                Origin = _origins[dense],
                Linked = _linked[dense],
            };
        }

        private int AllocSlot()
        {
            if (_freeSlotCount > 0)
                return _freeSlots[--_freeSlotCount];

            var slot = _slotHighWater++;
            _generations[slot] = 1;
            return slot;
        }

        private void EnsureCapacity(int required)
        {
            var capacity = _motions.Length;
            if (required <= capacity)
                return;

            while (capacity < required)
                capacity *= 2;
            Array.Resize(ref _motions, capacity);
            Array.Resize(ref _policies, capacity);
            Array.Resize(ref _states, capacity);
            Array.Resize(ref _sources, capacity);
            Array.Resize(ref _viewKeys, capacity);
            Array.Resize(ref _origins, capacity);
            Array.Resize(ref _linked, capacity);
            Array.Resize(ref _denseToSlot, capacity);
            Array.Resize(ref _slotToDense, capacity);
            Array.Resize(ref _generations, capacity);
            Array.Resize(ref _freeSlots, capacity);
            OnCapacityChanged(capacity);
        }
    }
}
