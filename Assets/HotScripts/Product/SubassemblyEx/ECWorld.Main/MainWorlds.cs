using Entitas;

namespace Xease.CoreGame
{
    public class MainWorlds : LiteUnityWorlds
    {
        protected override void CreateSystems()
        {
            base.CreateSystems();
            _rootSystem.Add(new SysGameplayInitialize_Main(this));
            //systems.Add(new SysDebugCoreGame(this));
            //systems.Add(new SysTimeScale(this));

        }
        
    }
}