using System;
using Xease.CoreGame;
using Xease.CoreGame.Debug;

namespace Xease.CoreGame
{
    public partial class WorldsConfig
    {
        private void InitConfigs_Main()
        {
            AddConfig("MainWorldTest", () => new MainWorldCreationInfo()
            {
                WorldName = "Main",
                WorldsClassType = typeof(MainWorlds),
                ModeLogicID = 1900001,
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
                    systems.Add(new SysMainFSM(worlds));
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
                    systems.Add(new SysLife(worlds.LogicWorld));
                    systems.Add(new SysDeathProcess(worlds));
                    systems.Add(new SysDebugDemo(worlds));
#if !CONSOLE_CLIENT
                    systems.Add(new SysDebugProfiler(worlds));
#endif

                    systems.Add(new SysGameplayInitialize_Main(worlds));
                    systems.Add(new UnitTestSystems_Base(worlds));
                    return systems;
                },
            });
        }
    }
}
