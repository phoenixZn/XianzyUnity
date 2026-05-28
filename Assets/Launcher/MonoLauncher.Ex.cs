using System.Collections.Generic;
using Newtonsoft.Json;
using UniFramework.Event;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// Launcher扩展部分 - 实现完整的启动流程
    /// 使用partial class扩展:
    /// </summary>
    public partial class MonoLauncher   // is MonoBehaviour
    {
        /// <summary>
        /// 资源系统运行模式
        /// </summary>
        public EPlayMode PlayMode = EPlayMode.EditorSimulateMode;

        /// <summary>
        /// 运行时设置
        /// </summary>
        public HybridRuntimeSettings RuntimeSettings;

        /// <summary>
        /// 运行时设置路径（用于网络加载）
        /// </summary>
        public string RuntimeSettingsPath;

        
        private readonly EventGroup _eventGroup = new ();
        private List<KeyValuePair<string, string>> _packageList = new List<KeyValuePair<string, string>>();
        private int _currentPackageIndex = 0;
        
        
        private void OnEnable()
        {
            // 注册事件监听
            _eventGroup.AddListener<UserEventDefine.UserTryInitialize>(OnHandleStateEvent);
            _eventGroup.AddListener<UserEventDefine.UserBeginDownloadWebFiles>(OnHandleStateEvent);
            _eventGroup.AddListener<UserEventDefine.UserTryRequestPackageVersion>(OnHandleStateEvent);
            _eventGroup.AddListener<UserEventDefine.UserTryUpdatePackageManifest>(OnHandleStateEvent);
            _eventGroup.AddListener<UserEventDefine.UserTryDownloadWebFiles>(OnHandleStateEvent);
            
            _eventGroup.AddListener<SceneEventDefine.ChangeToHomeScene>(OnHandleEventMessageEx);
            _eventGroup.AddListener<SceneEventDefine.ChangeToBattleScene>(OnHandleEventMessageEx);
        }

        private void OnDisable()
        {
            _eventGroup.RemoveAllListener();
        }

        //项目自行扩展 - 重写InitLaunchFSM实现完整流程
        private void InitLaunchFSM()
        {
            /*  完整的状态流程为：
                LS_InitApp → LS_LoadHybridRuntimeSettings（如果需要）→ LS_InitYooAsset → 
                LS_InitPackage → LS_RequestPackageVersion → LS_UpdatePackageManifest → 
                LS_CreateDownloader → LS_DownloadPackageFiles → LS_DownloadPackageOver → 
                LS_ClearCacheBundle → LS_EndPatch（循环处理多个包）→ 
                LS_LoadMetadataForAOTAssemblies → LS_LoadHotUpdateAssemblies → 
                LS_SetDefaultPackage → LS_StartGame
             */
            
            // 初始化App（游戏管理器、事件系统、加载更新页面）
            _fsm.AddState("LS_InitApp", new LStateInitApp());
            // 加载Hybrid运行时设置（如果需要）
            _fsm.AddState("LS_LoadHybridRuntimeSettings", new LStateLoadHybridRuntimeSettings());
            // 初始化YooAsset资源系统
            _fsm.AddState("LS_InitYooAsset", new LStateInitYooAsset());
            // 初始化资源包
            _fsm.AddState("LS_InitPackage", new LStateInitPackage());
            // 请求资源版本
            _fsm.AddState("LS_RequestPackageVersion", new LStateRequestPackageVersion());
            // 更新资源清单
            _fsm.AddState("LS_UpdatePackageManifest", new LStateUpdatePackageManifest());
            // 创建下载器
            _fsm.AddState("LS_CreateDownloader", new LStateCreateDownloader());
            // 下载资源文件
            _fsm.AddState("LS_DownloadPackageFiles", new LStateDownloadPackageFiles());
            // 下载完成
            _fsm.AddState("LS_DownloadPackageOver", new LStateDownloadPackageOver());
            // 清理缓存
            _fsm.AddState("LS_ClearCacheBundle", new LStateClearCacheBundle());
            // 补丁完成
            _fsm.AddState("LS_EndPatch", new LStateEndPatch());
            // 加载AOT元数据
            _fsm.AddState("LS_LoadMetadataForAOTAssemblies", new LStateLoadMetadataForAOTAssemblies());
            // 加载热更新程序集
            _fsm.AddState("LS_LoadHotUpdateAssemblies", new LStateLoadHotUpdateAssemblies());
            // 设置默认资源包
            _fsm.AddState("LS_SetDefaultPackage", new LStateSetDefaultPackage());
            // 启动游戏
            _fsm.AddState("LS_StartGame", new LStateStartGame());

            // 设置初始数据
            _fsm.SetBlackboardValue(LSVKey.LSV_PlayMode, PlayMode);
            _fsm.SetBlackboardValue(LSVKey.LSV_HybridRuntimeSettings, RuntimeSettings);
            _fsm.SetBlackboardValue(LSVKey.LSV_RuntimeSettingsPath, RuntimeSettingsPath);
            _fsm.SetBlackboardValue(LSVKey.LSV_ScriptPackageName, "PackMainScript");
            _fsm.SetBlackboardValue(LSVKey.LSV_GamePackageName, "PackDemoAsset");
            
            if (RuntimeSettings != null)
            {
                var packages = JsonConvert.DeserializeObject<Dictionary<string, string>>(RuntimeSettings.Packages);
                _packageList.Clear();
                foreach (var package in packages)
                {
                    _packageList.Add(package);
                }

                if (_packageList.Count > 0)
                {
                    var firstPackage = _packageList[0];
                    _fsm.SetBlackboardValue(LSVKey.LSV_PackageName, firstPackage.Key);
                    _fsm.SetBlackboardValue(LSVKey.LSV_Version, firstPackage.Value);
                    _fsm.SetBlackboardValue(LSVKey.LSV_CurrentPackageIndex, 0);
                    _fsm.SetBlackboardValue(LSVKey.LSV_TotalPackageCount, _packageList.Count);
                    _fsm.SetBlackboardValue(LSVKey.LSV_HasNextPackage, _packageList.Count > 1);
                    _fsm.SetBlackboardValue(LSVKey.LSV_IsPackageCompleted, false);
                    // 将包列表存入黑板，供状态机使用
                    _fsm.SetBlackboardValue(LSVKey.LSV_PackageList, _packageList);
                }
            }

            // 启动状态机
            _fsm.Start("LS_InitApp");
        }

        /// <summary>
        /// 接收事件
        /// </summary>
        private void OnHandleStateEvent(IEventMessage message)
        {
            if (message is UserEventDefine.UserTryInitialize)
            {
                _fsm.ForceChangeState("LS_InitPackage");
            }
            else if (message is UserEventDefine.UserBeginDownloadWebFiles)
            {
                _fsm.ForceChangeState("LS_DownloadPackageFiles");
            }
            else if (message is UserEventDefine.UserTryRequestPackageVersion)
            {
                _fsm.ForceChangeState("LS_RequestPackageVersion");
            }
            else if (message is UserEventDefine.UserTryUpdatePackageManifest)
            {
                _fsm.ForceChangeState("LS_UpdatePackageManifest");
            }
            else if (message is UserEventDefine.UserTryDownloadWebFiles)
            {
                _fsm.ForceChangeState("LS_CreateDownloader");
            }
            else
            {
                throw new System.NotImplementedException($"{message.GetType()}");
            }
        }

        private void OnHandleEventMessageEx(IEventMessage message)
        {
            if (message is SceneEventDefine.ChangeToHomeScene)
            {
                YooAssets.LoadSceneAsync("DemoHotScene");
            }
        }
        
        // 注意：包完成的逻辑在 LStateEndPatch 的 CheckTransitions 中处理
        // 通过状态机的自动转换机制，无需在Update中添加额外逻辑
    }

    //共享黑板键名（Launcher State Share Value）
    public partial class LSVKey
    {
        public const string LSV_PlayMode              = "LSV_PlayMode";              // YooAsset运行模式
        public const string LSV_HybridRuntimeSettings = "LSV_HybridRuntimeSettings"; // 混合运行时配置实例
        public const string LSV_RuntimeSettingsPath   = "LSV_RuntimeSettingsPath";   // 运行时配置加载路径
        public const string LSV_ScriptPackageName     = "LSV_ScriptPackageName";     // 热更程序集所在包名
        public const string LSV_GamePackageName       = "LSV_GamePackageName";       // 游戏默认资源包名
        public const string LSV_PackageName           = "LSV_PackageName";           // 当前补丁流程包名
        public const string LSV_Version               = "LSV_Version";               // 配置内包版本(拼CDN等)
        public const string LSV_CurrentPackageIndex   = "LSV_CurrentPackageIndex";   // 多包补丁当前下标
        public const string LSV_TotalPackageCount     = "LSV_TotalPackageCount";     // 多包补丁包总数
        public const string LSV_HasNextPackage        = "LSV_HasNextPackage";        // 是否还有下一包
        public const string LSV_IsPackageCompleted    = "LSV_IsPackageCompleted";    // 当前包补丁是否已标记完成
        public const string LSV_PackageList           = "LSV_PackageList";           // 多包包名与版本列表
        public const string LSV_PackageVersion        = "LSV_PackageVersion";        // 远端请求得到的包版本
        public const string LSV_Downloader            = "LSV_Downloader";            // 资源下载器操作句柄
    }
}
