using Xease.CoreGame;

namespace Xease
{
    public class MainWorldCreationInfo : WorldCreationInfo, IGameModeParam
    {
        public MainWorldCreationInfo() : base("Main")
        {
        }
        public int ModeLogicID { get; set; }
    }

    public class EnvMainState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            var modMainWorld = G.Module<ModuleMainWorld>();
            base.Enter(fromState);
            if (modMainWorld?.MainWorld == null)
            {
                var worldInfo = new MainWorldCreationInfo()
                {
                    ModeLogicID = 1100001,
                };
                modMainWorld.CreateGameWorld<MainWorlds>(worldInfo);
            }
            modMainWorld.SetActive(true);
        }
        
        public override void Leave(EnvStateBase toState)
        {
            var modMainWorld = G.Module<ModuleMainWorld>();
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
