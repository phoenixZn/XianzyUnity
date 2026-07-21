using Entitas;

namespace Xease.CoreGame
{
    public sealed class SysMainFSM : ECWorldSystem, IUpdateSystem  //IExecuteSystem
    {
        private readonly IGroup<LogicEntity> _group;

        public SysMainFSM(ECWorlds worlds) : base(worlds)
        {
            _group = _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComMainFSM));
        }

        // void IExecuteSystem.Execute()
        // {
        //     var dt = G.TickTime.deltaTime;
        //     foreach (var e in _group.GetEntities())
        //     {
        //         e.comFSM.Logic.Update(dt);
        //     }
        // }

        public void Update(float dt, float dt_unscaled)
        {
            foreach (var e in _group.GetEntities())
            {
                e.comFSM.Logic.Update(dt);
            }
        }
    }
}
