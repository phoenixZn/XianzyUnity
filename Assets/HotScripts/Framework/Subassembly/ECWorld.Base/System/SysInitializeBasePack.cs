using Entitas;

namespace Xease.CoreGame
{
    public class SysInitializeBasePack : InitializeSystem
    {
        public SysInitializeBasePack(ECWorlds worlds) : base(worlds)
        {
        }
        
        protected override void InitEntityIndex()
        {
            _logicWorld.AddEntityIndex_ComID();
            _logicWorld.AddEntityIndex(new EntityIndex<LogicEntity, EFaction>(typeof(FactionComponent).Name, _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComFaction)), (e, c) => ((FactionComponent)c).Faction));
            _logicWorld.AddEntityIndex(new EntityIndex<LogicEntity, long>(typeof(HolderEntityComponent).Name, _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComHolderEntity)), (e, c) => ((HolderEntityComponent)c).HolderEntityID));
            _logicWorld.AddEntityIndex(new EntityIndex<LogicEntity, uint>(typeof(TagComponent).Name, _logicWorld.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComTag)), (e, c) => ((TagComponent)c).Tags));
        }
        protected override void AddMetaComponents()
        {
        }
        protected override void RemoveMetaComponents()
        {
        }
    }
}