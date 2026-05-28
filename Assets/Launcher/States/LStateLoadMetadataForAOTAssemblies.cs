using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using HybridCLR;
using Newtonsoft.Json;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 加载补充元数据的AOT DLL状态
    /// </summary>
    public class LStateLoadMetadataForAOTAssemblies : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;
        private bool _isSuccess = false;

        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("加载AOT元数据！");
            _isCompleted = false;
            _nextStateID = null;
            _isSuccess = false;
            LoadMetadataForAOTAssemblies().Forget();
        }

        public override void Leave()
        {
            base.Leave();
            _isCompleted = false;
            _nextStateID = null;
            _isSuccess = false;
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }

        public override string CheckTransitions()
        {
            if (_isCompleted)
            {
                if (_isSuccess)
                {
                    return _nextStateID ?? "LS_LoadHotUpdateAssemblies";
                }
                else
                {
                    // 加载失败，保持当前状态（可以通过事件重试）
                    return _stateID;
                }
            }
            return _stateID; // 保持当前状态
        }

        async UniTaskVoid LoadMetadataForAOTAssemblies()
        {
            HomologousImageMode mode = HomologousImageMode.SuperSet;
            
            var scriptPackageName = _contextRef.GetBlackboardValue(LSVKey.LSV_ScriptPackageName, "PackMainScript");
            var scriptPackage = YooAssets.GetPackage((string)scriptPackageName);
            
            if (scriptPackage == null)
            {
                _contextRef.LogError($"xCore: LState LoadMetadataForAOTAssemblies Package not found: {scriptPackageName}");
                _isCompleted = true;
                return;
            }

            var handle = scriptPackage.LoadRawFileSync("AOTDLLs");
            await handle;
            if (handle.Status != EOperationStatus.Succeed)
            {
                _contextRef.LogError($"xCore: LState AOTDLLs LoadRawFileSync {handle.LastError}");
                _isCompleted = true;
                return;
            }

            var data = handle.GetRawFileText();
            if (string.IsNullOrEmpty(data))
            {
                _contextRef.LogError("AOTDLLs is null or empty");
                _isCompleted = true;
                return;
            }

            var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
            foreach (var name in dllNames)
            {
                var dataHandle = scriptPackage.LoadRawFileAsync(name);
                await dataHandle.ToUniTask();
                var dllData = dataHandle.GetRawFileData();
                if (dllData == null || dllData.Length == 0)
                {
                    _contextRef.LogError($"xCore: LState {name} is null or empty");
                    continue;
                }
        
                // 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
                LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllData, mode);
                var metaLog = $"xCore: LState LoadMetadataForAOTAssembly:{name}. mode:{mode} ret:{err}";
                if (err == LoadImageErrorCode.OK)
                    _contextRef.LogInfo(metaLog);
                else
                    _contextRef.LogError(metaLog);
            }

            _isSuccess = true;
            _nextStateID = "LS_LoadHotUpdateAssemblies";
            _isCompleted = true;
        }
    }
}
