using System;
using System.Collections.Generic;

namespace HotUpdate
{
    //GEnv 快捷访问静态类： 满足快捷访问的需求，留下未来重构的路径
    public static partial class G
    {
        public static T GetModule<T>() where T : class, IModule
        {
            return GEnv.Inst.Modules.GetModule<T>();
        }

        public static GEnvLogAction LogError => GEnv.Inst.Param.LogError;
        public static GEnvLogAction Log  => GEnv.Inst.Param.LogInfo;
    }
    
    //////////////////////////////////////////////////////////////////////////
    /// 游戏全局环境:
    /// * 纯逻辑中心，和Unity没有直接关联、依赖。
    //////////////////////////////////////////////////////////////////////////
    public partial class GEnv
    {
        protected static GEnv mInstance = null;
        public static GEnv Inst => mInstance;

        public GEnvParam Param { get; }

        //Layer1：框架性服务： （受到跨项目接口约束，偏底层、独立工作。 对外基本无依赖，有依赖也会在初始化时醒目的注入）
        protected List<IService> mServices = new();

        //Layer2：模块管理器： （高层逻辑模块、偏数据、偏业务、偏项目特化、协同工作、可以依赖其他Service、Module。对外依赖不受框架管理）
        protected IModuleManager mModules;
        public IModuleManager Modules
        {
            get { return mModules; }
        }

        //Layer3：其他自由管理器： （框架无限制，无依赖，项目随意）

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
            if (mInstance != null)
            {
                param.LogError("Core InitGameEnvInstance mInstance != null");
                return false;
            }

            mInstance = imp;
            imp.Inner_InitializeEnv();
            return true;
        }

        public virtual void Shutdown()
        {
            for (int i = mServices.Count - 1; i >= 0; i--)
            {
                mServices[i].Reset();
            }
            mModules?.Shutdown();
            mInstance = null;
        }

        public virtual void PreClear()
        {
            Inner_PreClear();
        }

        public virtual void Update(float deltaTime, float unscaledDeltaTime)
        {
            foreach (IService svc in mServices)
            {
                if (svc is IEnvTick tickSvc)
                {
                    tickSvc.Update(deltaTime, unscaledDeltaTime);
                }
            }
            Inner_UpdateEnv(deltaTime, unscaledDeltaTime);
        }

        public virtual void LateUpdate()
        {
            Inner_LateUpdateEnv();
        }

        public virtual void DrawGizmos()
        {
            Inner_DrawGizmosEnv();
        }

        protected virtual void Inner_InitializeEnv()
        {
            Inner_CreateServices(); //1、项目子类/扩展初始化 Services
            Inner_CreateManagers(); //2、项目子类/扩展初始化 Managers
            Inner_CreateModules();  //3、项目子类/扩展初始化 Modules
        }

        private T AddService<T>(IService service, out T getter) where T : class
        {
            getter = service as T;
            if (getter == null)
            {
                Param.LogError("GEnv AddService getter == null");
                return null;
            }
            mServices.Add(service);
            return getter;
        }

        protected virtual void Inner_CreateServices()
        {
        }

        protected virtual void Inner_CreateModules()
        {
        }

        protected virtual void Inner_CreateManagers()
        {
        }

        protected virtual void Inner_UpdateEnv(float deltaTime, float unscaledDeltaTime)
        {
        }

        protected virtual void Inner_LateUpdateEnv()
        {
        }

        protected virtual void Inner_DrawGizmosEnv()
        {
        }

        protected virtual void Inner_PreClear()
        {
        }
    }
}
