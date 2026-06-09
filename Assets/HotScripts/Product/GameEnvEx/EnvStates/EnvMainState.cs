namespace Xease
{
    public class EnvMainState : EnvStateBase
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
        }
        
        public override void Leave(EnvStateBase toState)
        {
            base.Leave(toState);
        }
        
        public override void EnvUpdate(float dt, float dt_unscaled)
        {
            base.EnvUpdate(dt, dt_unscaled);
        }
        
        public override string CheckTransitions()
        {
            return null;
        }
    }
}