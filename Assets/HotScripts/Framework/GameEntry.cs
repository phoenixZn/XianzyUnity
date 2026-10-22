#if !CONSOLE_CLIENT
using UnityEngine;
#else
using System;
using LitMotion;
#endif

namespace Xease
{
    /// <summary>
    /// 游戏入口。Unity 上由 PlayerLoop 驱动；命令行由宿主调用 CLI_Init 与帧函数。
    /// 环境实例为 GEnvProduct（Unity 与命令行共用，服务注册按 CONSOLE_CLIENT 分叉）。
    /// </summary>
#if !CONSOLE_CLIENT
    [AddComponentMenu("")]
    public class GameEntry : MonoBehaviour
#else
    public class GameEntry
#endif
    {
#if !CONSOLE_CLIENT
        static int sGameEntryInitAcc = 0; // GameEntryInit 调用次数，便于对照日志
        /// <summary>
        /// 创建或取回 GameEntry。供 AOT 程序集反射调用。
        /// </summary>
        public static GameEntry GameEntryInit()
        {
            Debug.Log($"GameEntryInit InitAcc={++sGameEntryInitAcc}");
            var gameEntry = FindObjectOfType<GameEntry>();
            if (gameEntry == null)
            {
                GameObject go = new GameObject("[GameEntry]");
                gameEntry = go.AddComponent<GameEntry>();
                DontDestroyOnLoad(go);
            }
            return gameEntry;
        }
#else
        /// <summary>
        /// 命令行初始化：完成 Awake/Start（含 GEnv 创建）。
        /// </summary>
        public static GameEntry GameEntryInit()
        {
            var gameEntry = new GameEntry();
            gameEntry.Awake();
            gameEntry.Start();
            return gameEntry;
        }
        
        /// <summary>
        /// 由命令行主循环退出时调用, 平替驱动 OnDestroy。
        /// </summary>
        public void DestroyEntry()
        {
            OnDestroy();
        }
#endif
        
        void TryCreateGameEnv()
        {
            if (GEnv.Inst != null)
            {
                return;
            }
#if !CONSOLE_CLIENT
            Debug.Log("初始化游戏环境 GEnv");
            var param = new GEnvParam()
            {
                LogInfo = Debug.Log,
                LogWarning = Debug.LogWarning,
                LogError = Debug.LogError,
                EnvBaseSeed = Time.frameCount,
                UnityHost = this,
            };
#else
            ConsoleHostBridge.Write("初始化游戏环境 GEnv");
            var param = new GEnvParam()
            {
                LogInfo = ConsoleHostBridge.Write,
                LogWarning = msg => ConsoleHostBridge.Write($"[Warning] {msg}"),
                LogError = msg => ConsoleHostBridge.Write($"[Error] {msg}"),
                EnvBaseSeed = Environment.TickCount,
            };
#endif
            GEnv.InitGameEnvInstance(new GEnvProduct(param));
        }


        //////////////////////////////////////////////////////////////////////////
        /// MonoBehaviour:
        /// 
        void Awake()
        {
#if CONSOLE_CLIENT
            // CLI 无 PlayerLoop：任何 LMotion.Create 之前切到 Manual，并接管未处理异常
            MotionScheduler.DefaultScheduler = MotionScheduler.Manual;
            MotionDispatcher.RegisterUnhandledExceptionHandler(ex => ConsoleHostBridge.Write(ex.ToString()));
#endif
        }

        void Start()
        {
            TryCreateGameEnv();
        }
        
        
        void OnDestroy()
        {
            GEnv.Inst?.DestroyEnv();
        }
        

        /// <summary>转发 GEnv.EnvFixUpdate。</summary>
        public void FixedUpdate()
        {
            GEnv.Inst?.EnvFixUpdate();
        }

        /// <summary>转发 GEnv.EnvUpdate。命令行先泵 Manual Motion。</summary>
        public void Update()
        {
#if CONSOLE_CLIENT
            if (GEnv.Inst != null)
            {
                ManualMotionDispatcher.Default.Update(G.deltaTime);
            }
#endif
            GEnv.Inst?.EnvUpdate();
        }

        /// <summary>转发 GEnv.EnvLateUpdate。</summary>
        public void LateUpdate()
        {
            GEnv.Inst?.EnvLateUpdate();
        }


        /// <summary>转发 GEnv.EnvDrawGizmos。</summary>
        public void OnDrawGizmos()
        {
            GEnv.Inst?.EnvDrawGizmos();
        }

#if DEMO_DEBUG
        void OnGUI()
        {
            GEnv.Inst?.OnEnvGUI();
        }
#endif

        void OnApplicationPause(bool pause)
        {
            GEnv.Inst?.OnEnvApplicationPause(pause);
        }

        void OnApplicationFocus(bool focus)
        {
            GEnv.Inst?.OnEnvApplicationFocus(focus);
        }

        /// <summary>Unity 应用退出消息入口。</summary>
        public void OnApplicationQuit()
        {
        }

    }
}
