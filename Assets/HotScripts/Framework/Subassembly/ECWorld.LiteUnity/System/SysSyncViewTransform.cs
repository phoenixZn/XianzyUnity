using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public sealed class SysSyncViewTransform : ReactiveSystem<LogicEntity>
    {
        public SysSyncViewTransform(ECWorlds worlds) : base(worlds.LogicWorld)
        {
        }

        protected override ICollector<LogicEntity> GetTrigger(IContext<LogicEntity> context)
        {
            return context.CreateCollector(LogicMatcher.AllOf(LogicComponentsLookup.ComTransform));
        }

        protected override bool Filter(LogicEntity entity)
        {
            if (!entity.hasComTransform || !entity.hasComView)
                return false;
            return entity.comView.HasSyncTransform;
        }

        protected override void Execute(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                SysViewLoader.SyncTransformFromEntity(entity);
            }
        }
    }
}
