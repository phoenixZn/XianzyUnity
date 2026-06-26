
namespace Xease
{
    public class EnvInitState : EnvStateBase
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
            G.Log("Core EnvInitState Enter");
        }
        
        public override void Leave(EnvStateBase toState)
        {
            G.Log("Core EnvInitState Leave");
            base.Leave(toState);
        }

        public override string CheckTransitions()
        {
            return EnvStateID.ES_Login;
        }
    }
}
