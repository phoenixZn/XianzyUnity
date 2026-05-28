using UnityEngine;

namespace Launcher
{
    // 启动器入口
    public partial class MonoLauncher : MonoBehaviour
    {
        private static MonoLauncher _instance = null;
        public static MonoLauncher Instance
        {
            get
            {
                if (!Application.isPlaying)
                    return null;
                return _instance;
            }
        }

        //启动步骤状态机
        protected LauncherFSM _fsm;

        private void Awake()
        {
            _instance = this;
            _fsm = new LauncherFSM();
            _fsm.InitByUnityEngine(this);
            
            // 初始化应用设置（与HybridLauncher保持一致）
            Debug.Log($"xCore: Launcher 资源系统运行模式：{PlayMode}");
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
            DontDestroyOnLoad(this.gameObject);
        }

        void Start()
        {
            InitLaunchFSM();
        }

        void Update()
        {
            _fsm.Update(Time.deltaTime);
        }

        //项目自行扩展
        // 注意：实际实现在 Launcher.partial.cs 中
        // private void InitLaunchFSM()
        // {
        //     //_fsm.AddState("LS_InitApp", new LStateInitializeApp());
        //     //_fsm.AddState("LS_InitYooAsset", new LStateInitYooAsset());
        //     // ... 待扩展补全 Launcher其余步骤
        //     //_fsm.AddState("LS_LoadHotUpdateAssembly", new LStateLoadHotUpdateAssembly());
        //     //_fsm.AddState("LS_StartGame", new LStateStartGame());
        //     // _fsm.Start("LS_InitApp");
        // }
    }
}
