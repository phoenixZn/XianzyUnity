using Xease.CoreGame;

namespace Xease
{
    public class EnvBattleState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);

            var modMainWorld = G.Module<ModuleWorlds>();
            if (modMainWorld.MainWorld != null)
            {
                G.LogError("EnvBattleState.Enter modMainWorld.MainWorld != null");
                modMainWorld.DestroyGameWorld();
            }
            
            //worldInfo 静态配置部分
            var worldInfo = modMainWorld.WorldsConfig.Get("MainWorldTest");
            if (worldInfo == null)
            {
                G.LogError("EnvBattleState.Enter WorldsConfig Get worldInfo == null");
                return;
            }
            //TODO：worldInfo 动态的部分加入
            if (worldInfo is MainWorldCreationInfo mainWorldCreationInfo)
            {
                mainWorldCreationInfo.LocalPlayer = new InGamePlayerInfo();
            }
            modMainWorld.CreateGameWorld(worldInfo);
            modMainWorld.SetActive(true);

#if !CONSOLE_CLIENT
            // 命令行宿主不注册 UI 服务
            G.UI.Show(UIPanelName.UIBattle);
#endif
        }
        
        public override void Leave(EnvStateBase toState)
        {
            var modMainWorld = G.Module<ModuleWorlds>();
            modMainWorld?.SetActive(false);
            modMainWorld?.DestroyGameWorld();

#if !CONSOLE_CLIENT
            G.UI.Hide(UIPanelName.UIBattle);
#endif
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