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