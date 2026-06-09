
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
            if(G.IsDev)
                G.Log("Core EnvInitState Leave");
            base.Leave(toState);
        }
        
        public override void Update(float dt)
        {
            base.Update(dt);
        }
        
        public override string CheckTransitions()
        {
            return EnvStateID.ES_Login;
        }
    }
}
