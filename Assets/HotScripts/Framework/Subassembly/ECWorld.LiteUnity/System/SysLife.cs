using Entitas;

namespace Xease.CoreGame
{
    public class SysLife : IExecuteSystem
    {
        private readonly LogicWorld mWorld;
        private readonly IGroup<LogicEntity> mGroup;
        
        public SysLife(LogicWorld logicWorld)
        {
            mWorld = logicWorld;
            mGroup = mWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComLife));
        }
        
        public void Execute()
        {
            var dt = G.TickTime.deltaTime;
            foreach (var e in mGroup.GetEntities())
            {
                var comLife = e.comLife;
                if (comLife.Duration > 0)
                {
                    comLife.Duration -= dt;
                }
                else
                {
                    e.RemoveComLife();
                    e.AddComDeath(null);
                }
            }
        }
    }
}