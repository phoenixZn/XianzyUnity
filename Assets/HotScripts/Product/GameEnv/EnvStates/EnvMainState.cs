using Xease.CoreGame;

namespace Xease
{
    
    public class EnvMainState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
#if !CONSOLE_CLIENT
            // 命令行宿主不注册 UI 服务
            G.UI.Show(UIPanelName.UIMain);
#endif
        }
        
        public override void Leave(EnvStateBase toState)
        {
#if !CONSOLE_CLIENT
            G.UI.Hide(UIPanelName.UIMain);
#endif
            base.Leave(toState);
        }
        
        public void EnvUpdate(float dt, float dt_unscaled)
        {
        }
        
        public override string CheckTransitions()
        {
#if CONSOLE_CLIENT
            return EnvStateID.ES_Battle;
#else
            return null;
#endif
        }
    }
}
