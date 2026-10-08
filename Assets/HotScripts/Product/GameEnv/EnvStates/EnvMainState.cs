using Xease.CoreGame;

namespace Xease
{
    
    public class EnvMainState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
            G.UI.Show(UIPanelName.UIMain);
        }
        
        public override void Leave(EnvStateBase toState)
        {
            G.UI.Hide(UIPanelName.UIMain);
            base.Leave(toState);
        }
        
        public void EnvUpdate(float dt, float dt_unscaled)
        {
        }
        
        public override string CheckTransitions()
        {
            return null;
        }
    }
}
