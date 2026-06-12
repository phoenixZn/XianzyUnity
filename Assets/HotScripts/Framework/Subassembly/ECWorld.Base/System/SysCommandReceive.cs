using Entitas;
using System.Collections.Generic;

namespace Xease.CoreGame
{
    public sealed class SysCommandReceive : ReactiveSystem<LogicEntity>
    {
        private ECWorlds _worlds;

        public SysCommandReceive(ECWorlds world) : base(world.LogicWorld)
        {
            _worlds = world;
        }

        protected override ICollector<LogicEntity> GetTrigger(IContext<LogicEntity> context)
        {
            return context.CreateCollector(LogicMatcher.AllOf(LogicComponentsLookup.ComCommandReceiver));
        }

        protected override bool Filter(LogicEntity entity)
        {
            return entity.hasComCommandReceiver;
        }

        protected override void Execute(List<LogicEntity> entities)
        {
            foreach (var e in entities)
            {
                e.comCommandReceiver.Dispatch();
            }
        }
    }
}