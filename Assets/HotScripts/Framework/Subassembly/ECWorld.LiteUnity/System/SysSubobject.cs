using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    /// <summary>
    /// 子物体驱动系统：初始化时创建 UniSubobjectsComponent，每帧在 Update 中按固定顺序驱动
    /// 实体层 Logic、目标快照、各套路批处理、命中派发、关联实体同步、表现同步、回收与排队生成。
    /// 应排在 SysBuff 之后、SysLife 与 SysDeathProcess 之前，使本帧挂上的 ComDeath 在同帧处理。
    /// </summary>
    public sealed partial class SysSubobject : ECWorldSystem, IInitializeSystem, IUpdateSystem, ITearDownSystem
    {
        private readonly IGroup<LogicEntity> _subobjectGroup;
        private readonly IGroup<LogicEntity> _hitBodyGroup;
        // 按 Group.CacheVersion 复用，避免每帧分配
        private readonly List<LogicEntity> _subobjectBuffer = new(256);
        private readonly List<LogicEntity> _hitBodyBuffer = new(256);
        // 与 Group.CacheVersion 对齐，-1 保证首次必填充
        private int _subobjectBufferVersion = -1;
        private int _hitBodyBufferVersion = -1;

        public SysSubobject(ECWorlds worlds) : base(worlds)
        {
            _subobjectGroup = _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComSubobject));
            _hitBodyGroup = _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComHitBody, LogicComponentsLookup.ComTransform));
        }

        //////////////////////////////////////////////////////////////////////////
        /// IInitializeSystem / IUpdateSystem / ITearDownSystem:

        public void Initialize()
        {
            _metaWorld.SetComUniSubobjects(_logicWorld);
        }

        public void Update(float dt, float dt_unscaled)
        {
            var subobjects = _metaWorld.comUniSubobjects;
            if (subobjects == null)
                return;

            subobjects.AdvanceTime(dt);
            TickLogics(dt);
            subobjects.RebuildTargets(_hitBodyGroup.GetEntities(_hitBodyBuffer, ref _hitBodyBufferVersion));
            subobjects.StepRoutines(dt);
            subobjects.DispatchHits();
            subobjects.SyncLinkedEntities();
            subobjects.SyncViews();
            subobjects.RecycleExpired();
            subobjects.FlushPendingSpawns();
        }

        public void TearDown()
        {
            _metaWorld.RemoveComUniSubobjects();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 只驱动仍存活且未进入死亡流程的实体子物体
        private void TickLogics(float dt)
        {
            var buffer = _subobjectGroup.GetEntities(_subobjectBuffer, ref _subobjectBufferVersion);
            for (var i = 0; i < buffer.Count; i++)
            {
                var entity = buffer[i];
                if (!entity.isEnabled || entity.hasComDeath || !entity.hasComSubobject)
                    continue;
                entity.comSubobject.Logic?.Update(dt);
            }
        }
    }
}
