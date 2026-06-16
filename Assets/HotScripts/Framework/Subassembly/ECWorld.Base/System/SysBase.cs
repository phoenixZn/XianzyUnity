using System.Collections.Generic;
using Entitas;
//////////////////////////////////////////////////////////////////////////
// 非标准，非必须，仅提供便捷


namespace Xease.CoreGame
{
    //////////////////////////////////////////////////////////////////////////
    public class ECWorldSystem : ISystem
    {
        protected ECWorlds _worlds;
        protected MetaWorld _metaWorld => _worlds.MetaWorld;
        protected LogicWorld _logicWorld => _worlds.LogicWorld;
        
        public ECWorldSystem(ECWorlds worlds)
        {
            _worlds = worlds;
        }
    }
    
    
    //////////////////////////////////////////////////////////////////////////
    public abstract class MetaReactiveSystem : ReactiveSystem<MetaEntity>
    {
        protected ECWorlds _worlds;
        protected MetaWorld _metaWorld => _worlds.MetaWorld;
        protected LogicWorld _logicWorld => _worlds.LogicWorld;
        
        public MetaReactiveSystem(ECWorlds worlds) : base(worlds.MetaWorld)
        {
            _worlds = worlds;
        }
        // protected abstract override ICollector<MetaEntity> GetTrigger(IContext<MetaEntity> context);
        //
        // protected abstract override bool Filter(MetaEntity entity);
        //
        // protected abstract override void Execute(List<MetaEntity> entities);
    }

    
    //////////////////////////////////////////////////////////////////////////
    public abstract class LogicReactiveSystem : ReactiveSystem<LogicEntity>
    {
        protected ECWorlds _worlds;
        protected MetaWorld _metaWorld => _worlds.MetaWorld;
        protected LogicWorld _logicWorld => _worlds.LogicWorld;
        
        public LogicReactiveSystem(ECWorlds worlds) : base(worlds.LogicWorld)
        {
            _worlds = worlds;
        }
        // protected abstract override ICollector<LogicEntity> GetTrigger(IContext<LogicEntity> context);
        // protected abstract override bool Filter(LogicEntity entity);
        // protected abstract override void Execute(List<LogicEntity> entities);
    }
    
    
    //////////////////////////////////////////////////////////////////////////
    public class InitializeSystem : ECWorldSystem, IInitializeSystem, ITearDownSystem
    {
        public InitializeSystem(ECWorlds worlds) : base(worlds)
        {
        }

        public void Initialize()
        {
            InitEntityIndex();
            AddMetaComponents();
        }

        public void TearDown()
        {
            RemoveMetaComponents();
        }

        //////////////////////////////////////////////////////////////////////////
        /// 按需重载
        protected virtual void InitEntityIndex()
        {
        }
        protected virtual void AddMetaComponents()
        {
        }
        protected virtual void RemoveMetaComponents()
        {
        }
    }
    
    
}