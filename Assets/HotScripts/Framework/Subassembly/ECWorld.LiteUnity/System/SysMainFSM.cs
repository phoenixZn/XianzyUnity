using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public sealed class SysMainFSM : ECWorldSystem, IUpdateSystem  //IExecuteSystem
    {
        private readonly IGroup<LogicEntity> _group;
        // 按 Group.CacheVersion 复用，避免每帧分配
        private readonly List<LogicEntity> _entityBuffer = new(256);
        // 与 Group.CacheVersion 对齐，-1 保证首次必填充
        private int _entityBufferVersion = -1;

        public SysMainFSM(ECWorlds worlds) : base(worlds)
        {
            _group = _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComMainFSM));
        }

        // public void Execute()
        // {
        //     var dt = G.TickTime.deltaTime;
        //     var buffer = _group.GetEntities(_entityBuffer, ref _entityBufferVersion);
        //     foreach (var e in buffer)
        //     {
        //         e.comFSM.Logic.Update(dt);
        //     }
        // }

        public void Update(float dt, float dt_unscaled)
        {
            var buffer = _group.GetEntities(_entityBuffer, ref _entityBufferVersion);
            foreach (var e in buffer)
            {
                e.comFSM.Logic.Update(dt);
            }
        }
    }
}
