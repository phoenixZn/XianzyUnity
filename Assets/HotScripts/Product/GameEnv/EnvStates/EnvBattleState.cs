namespace Xease
{
    public class EnvBattleState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
        }
        
        public override void Leave(EnvStateBase toState)
        {
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