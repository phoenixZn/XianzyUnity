using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 加载Hybrid运行时设置状态
    /// </summary>
    public class LStateLoadHybridRuntimeSettings : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("加载运行时设置！");
            _isCompleted = false;
            _nextStateID = null;
            LoadHybridRuntimeSettings().Forget();
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

        async UniTaskVoid LoadHybridRuntimeSettings()
        {
            var runtimeSettingsPath = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_RuntimeSettingsPath);
            if (string.IsNullOrEmpty(runtimeSettingsPath))
            {
                _contextRef.LogInfo("xCore: LState LoadHybridRuntimeSettings RuntimeSettingsPath == Null");
                _nextStateID = "LS_InitYooAsset";
                _isCompleted = true;
                return;
            }
            
            UnityWebRequest request = UnityWebRequest.Get(runtimeSettingsPath);
            request.timeout = 2;
            request.downloadHandler = new DownloadHandlerBuffer();
            await request.SendWebRequest();
            
            if (request.result != UnityWebRequest.Result.Success)
            {
                _contextRef.LogError($"xCore: LState LoadHybridRuntimeSettings Load Failed: {request.error}");
                _nextStateID = "LS_InitYooAsset";
                _isCompleted = true;
                return;
            }

            var data = request.downloadHandler.text;
            if (string.IsNullOrEmpty(data))
            {
                _contextRef.LogInfo("xCore: LState LoadHybridRuntimeSettings data is Null");
                _nextStateID = "LS_InitYooAsset";
                _isCompleted = true;
                return;
            }
            
            _contextRef.LogInfo($"xCore: LState LoadHybridRuntimeSettings download: {data}");
            var runtimeSettings = JsonConvert.DeserializeObject<HybridRuntimeSettings>(data);
            _contextRef.SetBlackboardValue(LSVKey.LSV_HybridRuntimeSettings, runtimeSettings);
            
            _nextStateID = "LS_InitYooAsset";
            _isCompleted = true;
        }
    }
}
