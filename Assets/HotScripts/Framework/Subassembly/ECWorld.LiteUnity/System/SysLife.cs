using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public class SysLife : ECWorldSystem, IUpdateSystem
    {
        private readonly IGroup<LogicEntity> _group;
        // 按 Group.CacheVersion 复用，避免每帧分配
        private readonly List<LogicEntity> _entityBuffer = new(256);
        // 与 Group.CacheVersion 对齐，-1 保证首次必填充
        private int _entityBufferVersion = -1;

        public SysLife(ECWorlds worlds) : base(worlds)
        {
            _group = _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComLife));
        }
        
        public void Update(float dt, float dt_unscaled)
        {
            var buffer = _group.GetEntities(_entityBuffer, ref _entityBufferVersion);
            foreach (var e in buffer)
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