using System;
using System.Collections.Generic;

namespace Xease
{
    //GEnv 快捷访问静态类： 满足快捷访问的需求，留下未来重构的路径
    public static partial class G
    {
        public static T GetModule<T>() where T : class, IModule
        {
            return GEnv.Inst.Modules().GetModule<T>();
        }

        public static GEnvLogAction LogError => GEnv.Inst.Param.LogError;
        public static GEnvLogAction LogWarning => GEnv.Inst.Param.LogWarning;
        public static GEnvLogAction Log  => GEnv.Inst.Param.LogInfo;
    }
    
    //////////////////////////////////////////////////////////////////////////
    /// 游戏全局环境:
    /// * 纯逻辑中心，和Unity没有直接关联、依赖。
    //////////////////////////////////////////////////////////////////////////
    public interface IGameEnv
    {
    }
    
    public partial class GEnv : IGameEnv
    {
        protected static GEnv sInstance = null;
        public static GEnv Inst => sInstance;

        public GEnvParam Param { get; }

        //Layer1：框架性服务： （受到跨项目接口约束，偏底层、独立工作。 对外基本无依赖，有依赖也会在初始化时醒目的注入）
        protected ServicesProvider _services;

        //Layer2：模块管理器： （高层逻辑模块、偏数据、偏业务、偏项目特化、协同工作、可以依赖其他Service、Module。对外依赖不受框架管理）
        protected ModuleManager _modules;

        //Layer3：其他自由管理器： （框架无限制，无依赖，项目随意）
        
        
        public ModuleManager Modules()
        {
            return _modules;
        }
        
        protected GEnv(GEnvParam param)
        {
            Param = param ?? throw new ArgumentNullException(nameof(param));
        }

        internal static bool InitGameEnvInstance(GEnv imp)
        {
            if (imp == null)
            {
                return false;
            }
            var param = imp.Param;
            param.LogInfo("Core InitGameEnvInstance");
            if (sInstance != null)
            {
                param.LogError("Core InitGameEnvInstance sInstance != null");
                return false;
            }

            sInstance = imp;
            imp.Inner_InitializeEnv();
            return true;
        }

        public virtual void DestroyEnv()
        {
            _modules?.Shutdown();
            _services?.Shutdown();

            sInstance = null;
        }

        public virtual void EnvUpdate()
        {
            float deltaTime = G.deltaTime;
            float unscaledDeltaTime = G.unscaledDeltaTime;
            _services?.Update(deltaTime, unscaledDeltaTime);
        }

        public virtual void EnvFixUpdate()
        {

        }
        
        public virtual void EnvLateUpdate()
        {

        }

        public virtual void EnvDrawGizmos()
        {

        }

        protected virtual void Inner_InitializeEnv()
        {
            Inner_CreateServices(); //项目子类/扩展初始化 Services
            Inner_CreateModules();  //项目子类/扩展初始化 Modules
            Inner_CreateManagers(); //项目子类/扩展初始化 Managers
            if (_services == null)
            {
                G.LogError("Inner_InitializeEnv _services == null");
            }
            if (_modules == null)
            {
                G.LogError("Inner_InitializeEnv _modules == null");
            }
        }

        private T AddService<T>(IService service, out T getter) where T : class
        {
            getter = service as T;
            if (getter == null)
            {
                Param.LogError("GEnv AddService getter == null");
                return null;
            }
            return _services.AddService(service, out getter);
        }

        protected virtual void Inner_CreateServices()
        {
            G.Log("[Core]: Env Services 初始化");
            _services = new ServicesProvider();
        }

        protected virtual void Inner_CreateModules()
        {
            G.Log("[Core]: Env Modules 初始化");
            _modules = new ModuleManager();
        }

        protected virtual void Inner_CreateManagers()
        {
        }
        
        
    }
}
