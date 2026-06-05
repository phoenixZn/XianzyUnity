using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
            AddService_TickTime();
            AddService_Random(Param.EnvBaseSeed);
            AddService_ValueEvent();
            AddService_Coroutine();
            AddService_Asset();
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
            _modules.Init(types);
            _modules.Start();
        }

        protected override void Inner_CreateManagers()
        {
        }
        
    }
}
