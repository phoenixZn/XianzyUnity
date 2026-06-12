using Entitas;

namespace Xease.CoreGame
{
    public class MainWorlds : LiteUnityWorlds
    {
        protected override void CreateSystems()
        {
            var systems = new UnityStyleSystems();

            // 初始化：
            systems.Add(new SysInitializeBasePack(this));
            systems.Add(new SysGameplayInitialize_Main(this));

            // 规则：
            //systems.Add(new SysGameModeUpdate(this));
            //systems.Add(new SysDebugCoreGame(this));
            //systems.Add(new SysTimeScale(this));

            // 输入：
            systems.Add(new SysCommandSend(this));
            systems.Add(new SysCommandReceive(this));
            
            // 处理流水线：
            //systems.Add(new SysSupplyProcess(this));
            //systems.Add(new SysAI(this));
            //systems.Add(new SysMainFSM(this));
            //systems.Add(new SysSkillProcess(this));

            //systems.Add(new SysLocomotion(this));
            //systems.Add(new SysCollision(this));

            //systems.Add(new SysSubobject(this));
            //systems.Add(new SysBuff(this));
            
            //systems.Add(new SysViewLoader(this));
            //systems.Add(new SysSyncViewTransform(this));
            //systems.Add(new SysSyncViewAnimator(this));
            //systems.Add(new SysLife(this));
            //systems.Add(new SysDeathProcess(this));


#if UNITY_EDITOR
            //systems.Add(new SysDebugDemo(this));
#endif
            _rootSystemUnity = systems;
            _rootSystem = _rootSystemUnity; 
        }
        
    }
}