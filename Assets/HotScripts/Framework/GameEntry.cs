using UnityEngine;

namespace Xease
{
    [AddComponentMenu("")]
    public partial class GameEntry : MonoBehaviour
    {
        
        static int sGameEntryInitAcc = 0;
        public static GameEntry GameEntryInit()  //主要供AOT程序集，反射调用
        {
            Debug.Log($"GameEntryInit InitAcc={++sGameEntryInitAcc}");
            if (!Application.isPlaying)
            {
                return null;
            }
            // 先查找是否已存在
            var _instance = FindObjectOfType<GameEntry>();
            if (_instance == null)
            {
                GameObject go = new GameObject("[GameEntry]");
                _instance = go.AddComponent<GameEntry>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
        
        
        void Awake()
        {
        }
        
        void Start()
        {
            if (GEnv.Inst != null)
            {
                return;
            }
            
            //TryCreateGameEnv -> GEnv.InitGameEnvInstance(new XXXGEnv(param));
            TryCreateGameEnv();
        }

        partial void TryCreateGameEnv();
        
        void OnDestroy()
        {
            Debug.Log("销毁游戏环境 GEnv");
            GEnv.Inst?.DestroyEnv();
        }
    
        //////////////////////////////////////////////////////////////////////////
        /// 引擎驱动逻辑环境入口

        public void FixedUpdate()
        {
            GEnv.Inst?.EnvFixUpdate();
        }

        public void Update()
        {
            GEnv.Inst?.EnvUpdate();
        }

        public void LateUpdate()
        {
            GEnv.Inst?.EnvLateUpdate();
        }

        public void OnDrawGizmos()
        {
            GEnv.Inst?.EnvDrawGizmos();
        }
        
        void OnGUI()
        {
            GEnv.Inst?.OnEnvGUI();
        }
        
        void OnApplicationPause(bool pause)
        {
            GEnv.Inst?.OnEnvApplicationPause(pause);
        }

        void OnApplicationFocus(bool focus)
        {
            GEnv.Inst?.OnEnvApplicationFocus(focus);
        }
        
        public void OnApplicationQuit()
        {
        }



    }
}