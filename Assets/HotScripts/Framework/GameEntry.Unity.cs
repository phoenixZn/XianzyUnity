using UnityEngine;



namespace HotUpdate
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
                    LogError = Debug.LogError,
                };
                GEnv.InitGameEnvInstance(new UnityGameEnv(param));
            }
        }
        
        void OnDestroy()
        {
            Debug.Log("销毁游戏环境 GEnv");
            GEnv.Inst?.Shutdown();
        }
    
        //////////////////////////////////////////////////////////////////////////
        /// 引擎驱动逻辑环境入口

        public void FixedUpdate()
        {
        }

        public void Update()
        {
            GEnv.Inst.Update(Time.deltaTime, Time.unscaledDeltaTime);
        }

        public void LateUpdate()
        {
            GEnv.Inst.LateUpdate();
        }

        public void OnApplicationQuit()
        {
        }

        public void OnDrawGizmos()
        {
            GEnv.Inst?.DrawGizmos();
        }
    }
}