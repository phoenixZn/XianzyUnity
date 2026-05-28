using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 初始化资源包状态
    /// </summary>
    public class LStateInitPackage : LauncherState
    {
        private HybridRuntimeSettings _runtimeSettings;
        private string _packageName;
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("初始化资源包！");
            _isCompleted = false;
            _nextStateID = null;
            InitPackage().Forget();
        }

        public override void Leave()
        {
            base.Leave();
            _isCompleted = false;
            _nextStateID = null;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }

        public override string CheckTransitions()
        {
            if (_isCompleted && _nextStateID != null)
            {
                return _nextStateID;
            }
            return _stateID; // 保持当前状态
        }

        async UniTaskVoid InitPackage()
        {
            var playMode = (EPlayMode)_contextRef.GetBlackboardValue(LSVKey.LSV_PlayMode);
            _packageName = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageName);
            _runtimeSettings = (HybridRuntimeSettings)_contextRef.GetBlackboardValue(LSVKey.LSV_HybridRuntimeSettings);

            // 创建资源包裹类
            var package = YooAssets.TryGetPackage(_packageName);
            if (package == null)
                package = YooAssets.CreatePackage(_packageName);

            // 编辑器下的模拟模式
            InitializationOperation initializationOperation = null;
            if (playMode == EPlayMode.EditorSimulateMode)
            {
                var buildResult = EditorSimulateModeHelper.SimulateBuild(_packageName);
                var packageRoot = buildResult.PackageRootDirectory;
                var createParameters = new EditorSimulateModeParameters();
                createParameters.EditorFileSystemParameters = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // 单机运行模式
            if (playMode == EPlayMode.OfflinePlayMode)
            {
                var createParameters = new OfflinePlayModeParameters();
                createParameters.BuildinFileSystemParameters = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // 联机运行模式
            if (playMode == EPlayMode.HostPlayMode)
            {
                string defaultHostServer = GetHostServerURL();
                string fallbackHostServer = GetHostServerURL();
                IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                var createParameters = new HostPlayModeParameters();
                createParameters.BuildinFileSystemParameters = null;
                createParameters.CacheFileSystemParameters = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                initializationOperation = package.InitializeAsync(createParameters);
            }

            // WebGL运行模式
            if (playMode == EPlayMode.WebPlayMode)
            {
#if UNITY_WEBGL && WEIXINMINIGAME && !UNITY_EDITOR
                var createParameters = new WebPlayModeParameters();
                string defaultHostServer = GetHostServerURL();
                string fallbackHostServer = GetHostServerURL();
                string packageRoot = $"{WeChatWASM.WX.env.USER_DATA_PATH}/__GAME_FILE_CACHE";
                IRemoteServices remoteServices = new RemoteServices(defaultHostServer, fallbackHostServer);
                createParameters.WebServerFileSystemParameters = WechatFileSystemCreater.CreateFileSystemParameters(packageRoot, remoteServices);
                initializationOperation = package.InitializeAsync(createParameters);
#else
                var createParameters = new WebPlayModeParameters();
                createParameters.WebServerFileSystemParameters = FileSystemParameters.CreateDefaultWebServerFileSystemParameters();
                initializationOperation = package.InitializeAsync(createParameters);
#endif
            }

            await initializationOperation;

            // 如果初始化失败弹出提示界面
            if (initializationOperation.Status != EOperationStatus.Succeed)
            {
                Debug.LogWarning($"{initializationOperation.Error}");
                PatchEventDefine.InitializeFailed.SendEventMessage();
            }
            else
            {
                _nextStateID = "LS_RequestPackageVersion";
            }
            _isCompleted = true;
        }

        /// <summary>
        /// 获取资源服务器地址
        /// </summary>
        private string GetHostServerURL()
        {
            string hostServerIP = _runtimeSettings.HostServerIP;
            string appVersion = _runtimeSettings.ReleaseBuildVersion.ToString();
            var packageVersion = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_Version);

#if UNITY_EDITOR
            var activeBuildTarget = UnityEditor.EditorUserBuildSettings.activeBuildTarget;
            var hostPath = Path.Combine(hostServerIP, "Bundles", appVersion, activeBuildTarget.ToString(), _packageName, packageVersion);
            Debug.unityLogger.Log(hostPath);
            return hostPath;
#else
            var activeBuildTarget = Application.platform;
            return Path.Combine(hostServerIP, "Bundles", appVersion, activeBuildTarget.ToString(), _packageName, packageVersion.ToString());
#endif
        }

        /// <summary>
        /// 远端资源地址查询服务类
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHostServer;
            private readonly string _fallbackHostServer;

            public RemoteServices(string defaultHostServer, string fallbackHostServer)
            {
                _defaultHostServer = defaultHostServer;
                _fallbackHostServer = fallbackHostServer;
            }
            string IRemoteServices.GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHostServer}/{fileName}";
            }
            string IRemoteServices.GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHostServer}/{fileName}";
            }
        }
    }
}
