using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xease.CoreGame;

namespace Xease
{
    /// <summary>
    /// 命令行宿主：对应 Unity 侧 GameEntry 的初始化与引擎驱动入口（无 MonoBehaviour）。
    /// </summary>
    public class GameEntry
    {
        static int sGameEntryInitAcc = 0; // GameEntryInit 调用次数，便于对照 Unity 日志
        static GameEntry sInstance; // 命令行单例，替代 FindObjectOfType

        /// <summary>
        /// 对应 Unity 的 GameEntryInit：创建单例并完成 Awake/Start（含 GEnv 初始化）。
        /// </summary>
        public static GameEntry GameEntryInit()
        {
            Console.WriteLine($"GameEntryInit InitAcc={++sGameEntryInitAcc}");
            if (sInstance == null)
            {
                sInstance = new GameEntry();
                sInstance.Awake();
                sInstance.Start();
            }
            return sInstance;
        }

        void Awake()
        {
        }

        void Start()
        {
            // TryCreateGameEnv -> GEnv.InitGameEnvInstance(new ConsoleGameEnv(param));
            TryCreateGameEnv();
        }

        void TryCreateGameEnv()
        {
            if (GEnv.Inst != null)
            {
                Console.WriteLine("[Error] TryCreateGameEnv GEnv.Inst != null");
                return;
            }
            Console.WriteLine("初始化游戏环境 GEnv");
            var param = new GEnvParam()
            {
                LogInfo = Console.WriteLine,
                LogWarning = msg => Console.WriteLine($"[Warning] {msg}"),
                LogError = msg => Console.WriteLine($"[Error] {msg}"),
                EnvBaseSeed = Environment.TickCount,
            };
            GEnv.InitGameEnvInstance(new ConsoleGameEnv(param));
        }

        /// <summary>
        /// 对应 Unity OnDestroy：销毁 GEnv 并清空单例。
        /// </summary>
        public void Destroy()
        {
            Console.WriteLine("销毁游戏环境 GEnv");
            GEnv.Inst?.DestroyEnv();
            sInstance = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// 引擎驱动逻辑环境入口

        /// <summary>
        /// 对应 Unity FixedUpdate，转发 GEnv.EnvFixUpdate。
        /// </summary>
        public void FixedUpdate()
        {
            GEnv.Inst?.EnvFixUpdate();
        }

        /// <summary>
        /// 对应 Unity Update，转发 GEnv.EnvUpdate。
        /// </summary>
        public void Update()
        {
            GEnv.Inst?.EnvUpdate();
        }

        /// <summary>
        /// 对应 Unity LateUpdate，转发 GEnv.EnvLateUpdate。
        /// </summary>
        public void LateUpdate()
        {
            GEnv.Inst?.EnvLateUpdate();
        }
    }

    /// <summary>
    /// 命令行宿主的 GEnv 实现，对应 UnityGameEnv；不注册 Asset/Input/Coroutine/GOPool。
    /// </summary>
    public class ConsoleGameEnv : GEnv
    {
        /// <summary>
        /// 使用注入的日志与随机种子构造命令行环境。
        /// </summary>
        public ConsoleGameEnv(GEnvParam param) : base(param)
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
            var svcLogic = Services.AddService_CustomLogic();
            svcLogic.AddConfigContainer(new LogicConfigs_GameMode(LogicContainerKey.LogicConfigs_GameMode));
            svcLogic.AddConfigContainer(new LogicConfigs_GameLevel(LogicContainerKey.LogicConfigs_GameLevel));
            svcLogic.AddConfigContainer(new LogicConfig_Skill(LogicContainerKey.LogicConfigs_Skill));
            svcLogic.AddConfigContainer(new LogicConfig_EntityFSM(LogicContainerKey.LogicConfigs_EntityFSM));
            svcLogic.AddConfigContainer(new LogicConfig_AI(LogicContainerKey.LogicConfigs_AI));
            svcLogic.AddConfigContainer(new LogicConfig_Subobject(LogicContainerKey.LogicConfigs_Subobject));
            svcLogic.AddConfigContainer(new LogicConfig_Buff(LogicContainerKey.LogicConfigs_Buff));
            svcLogic.AddConfigContainer(new LogicConfig_Supply(LogicContainerKey.LogicConfigs_Supply));
        }

        protected override void Inner_CreateModules()
        {
            base.Inner_CreateModules();

            // 自动收集全部 Module（本工程与 Program 同一程序集）
            Assembly assembly = Assembly.GetExecutingAssembly();
            var FullName_IModule = typeof(IModule).FullName;
            var types = GetLoadableTypes(assembly).Where(type => !type.IsInterface && !type.IsAbstract && type.GetInterface(FullName_IModule) != null);
            types = types.Where(t => t.GetCustomAttribute<SkipModuleAutoRegisterAttribute>() == null);

            Modules.Init(types);
            Modules.Start();
        }

        // CLI 程序集内仍有类型引用未加载的 Unity 模块，GetTypes 会抛 ReflectionTypeLoadException
        static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
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
