using Xease.CoreGame;

namespace Xease
{
    
    public class EnvMainState : EnvStateBase, IEnvUpdate
    {
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
            
            var modMainWorld = G.Module<ModuleMainWorld>();
            if (modMainWorld.MainWorld != null)
            {
                G.LogError("EnvMainState.Enter modMainWorld.MainWorld != null");
                modMainWorld.DestroyGameWorld();
            }
            
            var worldInfo = new MainWorldCreationInfo()
            {
                ModeLogicID = 1100001,
                CreateRootSystems = (info, worlds) =>
                {
                    var systems = new UnityStyleSystems();
                    // 初始化：
                    systems.Add(new SysInitializeBasePack(worlds));
                    systems.Add(new SysInitializeLiteUnityPack(worlds));
                    // 输入：
                    systems.Add(new SysCommandSend(worlds));
                    systems.Add(new SysCommandReceive(worlds));
                    // 业务流水线：
                    systems.Add(new SysGameModeUpdate(worlds));
                    //systems.Add(new SysSupplyProcess(worlds));
                    //systems.Add(new SysAI(worlds));
                    //systems.Add(new SysMainFSM(worlds));
                    //systems.Add(new SysSkillProcess(worlds));
                    //systems.Add(new SysLocomotion(worlds));
                    //systems.Add(new SysCollision(worlds));
                    //systems.Add(new SysSubobject(worlds));
                    //systems.Add(new SysBuff(worlds));
                    // 表现：
                    systems.Add(new SysViewLoader(worlds));
                    systems.Add(new SysSyncViewTransform(worlds));
                    //systems.Add(new SysSyncViewAnimator(worlds));
                    // 销毁：
                    //systems.Add(new SysLife(worlds));
                    //systems.Add(new SysDeathProcess(worlds));
                    //systems.Add(new SysDebugDemo(worlds));

                    systems.Add(new SysGameplayInitialize_Main(worlds));
                    systems.Add(new UnitTestSystems_Base(worlds));
                    return systems;
                },
            };
            modMainWorld.CreateGameWorld<MainWorlds>(worldInfo);
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
