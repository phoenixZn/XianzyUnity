using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xease.CoreGame;

//using UnityEngine;

namespace Xease
{
    // 实现约束下的快捷访问, 项目特化的不往里扔 （满足快捷访问的需求，留个方便重构的退路）
    public static partial class G
    {
    }

    //////////////////////////////////////////////////////////////////////////
    /// 游戏全局环境（Unity 宿主实现）
    //////////////////////////////////////////////////////////////////////////
    public class UnityGameEnv : GEnv
    {
        public UnityGameEnv(GEnvParam param) : base(param)
        {
        }

        protected override void Inner_CreateServices()
        {
            base.Inner_CreateServices();
            
            Services.AddService_TickTime();
            Services.AddService_Timer();
            Services.AddService_Random(Param.EnvBaseSeed);
            Services.AddService_ValueEvent();
            Services.AddService_SharedPool();
            Services.AddService_Coroutine(Param.UnityHost);
            Services.AddService_Asset();
            Services.AddService_GameObjectPools();
            Services.AddService_Input();
            var svcLogic = Services.AddService_CustomLogic();
            svcLogic.AddConfigContainer(new LogicConfigs_GameMode(LogicContainerKey.LogicConfigs_GameMode));
            svcLogic.AddConfigContainer(new LogicConfigs_GameLevel(LogicContainerKey.LogicConfigs_GameLevel));
            svcLogic.AddConfigContainer(new LogicConfig_Skill(LogicContainerKey.LogicConfigs_Skill));
            svcLogic.AddConfigContainer(new LogicConfig_EntityFSM(LogicContainerKey.LogicConfigs_EntityFSM));
            svcLogic.AddConfigContainer(new LogicConfig_AI(LogicContainerKey.LogicConfigs_AI));
            svcLogic.AddConfigContainer(new LogicConfig_Subobject(LogicContainerKey.LogicConfigs_Subobject));
            svcLogic.AddConfigContainer(new LogicConfig_Buff(LogicContainerKey.LogicConfigs_Buff));
            svcLogic.AddConfigContainer(new LogicConfig_Supply(LogicContainerKey.LogicConfigs_Supply));
            svcLogic.BaseWarmup();
        }

        protected override void Inner_CreateModules()
        {
            base.Inner_CreateModules();
            
            //自动收集全部 Module
            Assembly assembly = Assembly.GetExecutingAssembly();
            var FullName_IModule = typeof(IModule).FullName;
            var types = assembly.GetTypes().Where(type => !type.IsInterface && !type.IsAbstract && type.GetInterface(FullName_IModule) != null);
            types = types.Where(t => t.GetCustomAttribute<SkipModuleAutoRegisterAttribute>() == null);
            
            //初始化:
            Modules.Init(types);
            Modules.Start();
        }

        protected override void Inner_CreateManagers()
        {
            base.Inner_CreateManagers();
            
            Dictionary<string, EnvStateBase> states = new()
            {
                [EnvStateID.ES_EnvInit] = new EnvInitState(),
                [EnvStateID.ES_Login] = new EnvLoginState(),
                [EnvStateID.ES_Main] = new EnvMainState(),
            };
            EnvStateMng.Initialize(states, EnvStateID.ES_EnvInit);
        }
        
    }
}
