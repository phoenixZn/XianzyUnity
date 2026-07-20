using Xease.CoreGame;

namespace Xease
{
    
    public class EnvMainState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
            
            var modMainWorld = G.Module<ModuleWorlds>();
            if (modMainWorld.MainWorld != null)
            {
                G.LogError("EnvMainState.Enter modMainWorld.MainWorld != null");
                modMainWorld.DestroyGameWorld();
            }
            
            //worldInfo 静态配置部分
            var worldInfo = modMainWorld.WorldsConfig.Get("MainWorldTest");
            if (worldInfo == null)
            {
                G.LogError("EnvMainState.Enter WorldsConfig Get worldInfo == null");
                return;
            }
            //TODO：worldInfo 动态的部分加入
            modMainWorld.CreateGameWorld(worldInfo);
            modMainWorld.SetActive(true);
        }
        
        public override void Leave(EnvStateBase toState)
        {
            var modMainWorld = G.Module<ModuleWorlds>();
            modMainWorld?.SetActive(false);
            modMainWorld?.DestroyGameWorld();
            
            base.Leave(toState);
        }
        
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            //G.Log($"EnvUpdate[{StateID}]: dt={dt}, dt_unscaled={dt_unscaled}");
        }
        
        public override string CheckTransitions()
        {
            return null;
        }
    }
}
