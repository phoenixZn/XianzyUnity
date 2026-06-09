using System;
using System.Collections.Generic;

namespace Xease
{
    //GEnv 快捷访问静态类： 满足快捷访问的需求，留下未来重构的路径
    public static partial class G
    {
        public static T GetModule<T>() where T : class, IModule
        {
            return GEnv.Inst.Modules.GetModule<T>();
        }

        public static GEnvLogAction LogError => GEnv.Inst.Param.LogError;
        public static GEnvLogAction LogWarning => GEnv.Inst.Param.LogWarning;
        public static GEnvLogAction Log  => GEnv.Inst.Param.LogInfo;
        public static bool IsDev = true;
    }
    
    //////////////////////////////////////////////////////////////////////////
    /// 游戏全局环境: GameGlobalEnv
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

        //Layer1：框架性服务： （受到跨项目接口约束，偏底层、独立工作。 对外基本无依赖，有依赖也会在初始化时醒目的注入。过去通常被实现为单例的高内聚部件）
        public ServicesProvider Services { get; protected set; }

        //Layer2：模块管理器： （高层逻辑模块、偏数据、偏业务、偏项目特化、协同工作、可以依赖其他Service、Module。对外依赖不受框架管理）
        public ModuleManager Modules { get; protected set; }

        //Layer3：其他自由管理器： （框架无限制，无依赖，项目随意）
        public EnvStateManager EnvStateMng { get; protected set; }
        // ......

        protected EnvDriver _driver { get; set; }

        
        protected GEnv(GEnvParam param)
        {
            Param = param ?? throw new ArgumentNullException(nameof(param));
        }

        //////////////////////////////////////////////////////////////////////////
        /// 构造、销毁
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
            _driver?.UnBindEnvActions(Modules?.OuterDriver);
            _driver?.UnBindEnvActions(Services?.OuterDriver);
            _driver?.UnBindEnvActions(EnvStateMng?.OuterDriver);
            _driver.ClearAllBind();

            EnvStateMng?.Destroy();
            Modules?.Shutdown();
            Services?.Shutdown();

            sInstance = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// 驱动:
        public virtual void EnvUpdate()
        {
            _driver?.EnvUpdate(G.deltaTime, G.unscaledDeltaTime);
        }

        public virtual void EnvFixUpdate()
        {
            _driver?.EnvFixedUpdate(G.fixedDeltaTime, G.fixedUnscaledDeltaTime);
        }
        
        public virtual void EnvLateUpdate()
        {
            _driver?.EnvLateUpdate(G.deltaTime, G.unscaledDeltaTime);
        }

        public virtual void EnvDrawGizmos()
        {
            _driver?.EnvDrawGizmos();
        }
        
        public virtual void OnEnvGUI()
        {
            _driver?.OnEnvGUI();
        }

        public virtual void OnEnvApplicationPause(bool pause)
        {
            _driver?.OnEnvApplicationPause(pause);
        }

        public virtual void OnEnvApplicationFocus(bool focus)
        {
            _driver?.OnEnvApplicationFocus(focus);
        }

        //////////////////////////////////////////////////////////////////////////
        /// 扩展:
        protected virtual void Inner_InitializeEnv()
        {
            Inner_CreateServices(); //项目子类/扩展初始化 Services
            Inner_CreateModules();  //项目子类/扩展初始化 Modules
            Inner_CreateManagers(); //项目子类/扩展初始化 Managers
            if (Services == null)
            {
                G.LogError("Inner_InitializeEnv _services == null");
            }
            if (Modules == null)
            {
                G.LogError("Inner_InitializeEnv _modules == null");
            }

            LinkEnvActions();
        }

        protected virtual void LinkEnvActions()
        {
            _driver = new EnvDriver("GEnv");
            _driver.BindEnvActions(Modules?.OuterDriver);
            _driver.BindEnvActions(Services?.OuterDriver);
            _driver.BindEnvActions(EnvStateMng?.OuterDriver);
        }

        protected virtual void Inner_CreateServices()
        {
            Param.LogInfo("[Core]: Env Services 初始化");
            Services = new ServicesProvider();
        }

        protected virtual void Inner_CreateModules()
        {
            Param.LogInfo("[Core]: Env Modules 初始化");
            Modules = new ModuleManager();
        }

        protected virtual void Inner_CreateManagers()
        {

        }
        
        
    }
}
