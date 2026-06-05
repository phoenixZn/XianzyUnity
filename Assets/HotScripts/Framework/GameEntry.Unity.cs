using UnityEngine;



namespace Xease
{
    public partial class GameEntry : MonoBehaviour
    {
        void Awake()
        {
        }
        
        void Start()
        {
            if (GEnv.Inst == null)
            {
                Debug.Log("初始化游戏环境 GEnv");
                var param = new GEnvParam()
                {
                    LogInfo = Debug.Log,
                    LogWarning = Debug.LogWarning,
                    LogError = Debug.LogError,
                    EnvBaseSeed = Time.frameCount,
                };
                GEnv.InitGameEnvInstance(new UnityGameEnv(param));
            }
        }
        
        void OnDestroy()
        {
            Debug.Log("销毁游戏环境 GEnv");
            GEnv.Inst?.DestroyEnv();
        }
    
        //////////////////////////////////////////////////////////////////////////
        /// 引擎驱动逻辑环境入口

        public void FixedUpdate()
        {
            GEnv.Inst.EnvFixUpdate();
        }

        public void Update()
        {
            GEnv.Inst.EnvUpdate();
        }

        public void LateUpdate()
        {
            GEnv.Inst.EnvLateUpdate();
        }

        public void OnDrawGizmos()
        {
            GEnv.Inst?.EnvDrawGizmos();
        }
        
        public void OnApplicationQuit()
        {
        }
    }
}