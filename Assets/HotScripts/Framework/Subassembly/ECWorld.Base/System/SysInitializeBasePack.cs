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
            _logicWorld.AddEntityIndex_ComFaction();
            _logicWorld.AddEntityIndex_ComHolderEntity();
            _logicWorld.AddEntityIndex_ComTag();
        }
        protected override void AddMetaComponents()
        {
        }
        protected override void RemoveMetaComponents()
        {
        }
    }
}