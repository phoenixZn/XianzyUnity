using Entitas;

namespace Xease.CoreGame
{
    public class SysInitializeLiteUnityPack : InitializeSystem
    {
        public SysInitializeLiteUnityPack(ECWorlds worlds) : base(worlds)
        {
        }
        
        protected override void InitEntityIndex()
        {
            _logicWorld.AddEntityIndex_UnityObjectRelated();
        }
        protected override void AddMetaComponents()
        {
        }
        protected override void RemoveMetaComponents()
        {
        }
    }
}