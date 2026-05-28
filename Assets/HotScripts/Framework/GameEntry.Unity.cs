using UnityEngine;
/*
热更程序集结构说明：
├── HotUpdate/
│   ├── Framework/               跨项目框架约束共识 ----------------------------------------------------------------
│   │   ├── BaseUtility/         #公共程序集 （这里的代码所有项目，各处都依赖，都会引用，避免修改）
│   │   │	├── DataStruct/     （公共数据结构）
│   │   │	└── Helper/         （公共静态方法）
│   │   ├── CoreGame/            #核心玩法，局内战斗
│   │   ├── Modules/             #数据模块
│   │   ├── Services/            #有框架接口规范约束的、业务无关的底层服务（过去每个项目都有的 AssetMng、EventMng、TaskMng、NetMng、ConfigMng等等 ） 
│   │   │   ├── Interface/      （定义公司全项目复用的基础设施 接口层约束，IAssetService、IPBConfigService、INetService等）
│   │   ├── GEnv                 #全局环境，唯一官方单例，拼装所有的 Service、Module、Managers（过去的GameGlobal）
│   │   └── GameEntry       #AOT调用热更程序集的入口 （负责创建GEnv，驱动GEnv，和AOT程序集中的启动器交互）
│   │
│   │
│   ├── LiteGame/                小游戏扩展框架 ----------------------------------------------------------------
│   │   ├── BaseUtility/
│   │   ├── CoreGame/
│   │   │   ├── CustomNodes.Ex/      （CustomLogic的Nodes扩展）
│   │   │   ├── ECWorldEx/           （ECWorld扩展）
│   │   │   ├── ECWorldPack.Base/    （ECWorld基本组件包）
│   │   │   └── ECWorldPack.Entt/    （ECWorld扩展组件包.Entt关联）
│   │   ├── Modules/                  #数据模块
│   │   ├── Managers/                （GEnv中 不受框架限制的自由Mng)
│   │   ├── Services/                （GEnv中 受框架接口约束的Mng)
│   │   │   ├── Service.GameObjectPool/
│   │   │   ├── Service.UI/
│   │   │   └── Service.Entt/
│   │   ├── GEnv.LiteGame
│   │   └── GameEntry.LiteGame
 */


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