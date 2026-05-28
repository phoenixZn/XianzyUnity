using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 加载热更新DLL状态
    /// </summary>
    public class LStateLoadHotUpdateAssemblies : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;
        private bool _isSuccess = false;

        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("加载热更新程序集！");
            _isCompleted = false;
            _nextStateID = null;
            _isSuccess = false;
            LoadHotUpdateAssemblies().Forget();
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
                    return _nextStateID ?? "LS_SetDefaultPackage";
                }
                else
                {
                    // 加载失败，保持当前状态（可以通过事件重试）
                    return _stateID;
                }
            }
            return _stateID; // 保持当前状态
        }

        async UniTaskVoid LoadHotUpdateAssemblies()
        {
            var scriptPackageName = _contextRef.GetBlackboardValue(LSVKey.LSV_ScriptPackageName, "PackMainScript");
            var scriptPackage = YooAssets.GetPackage((string)scriptPackageName);
            
            if (scriptPackage == null)
            {
                _contextRef.LogError($"xCore: LState LoadHotUpdateAssemblies Package not found: {scriptPackageName}");
                _isCompleted = true;
                return;
            }
            
            var handle = scriptPackage.LoadRawFileSync(AppConfig.HotDLLsTxtFileName);
            await handle.ToUniTask();
            var data = handle.GetRawFileText();
            if (string.IsNullOrEmpty(data))
            {
                _contextRef.LogError("HotUpdateDLLs is null or empty");
                _isCompleted = true;
                return;
            }

            var dllNames = JsonConvert.DeserializeObject<List<string>>(data);
            foreach (var dllName in dllNames)
            {
                var dataHandle = scriptPackage.LoadRawFileAsync(dllName);
                await dataHandle.ToUniTask();
                if (dataHandle.Status != EOperationStatus.Succeed)
                {
                    _contextRef.LogError($"xCore: LState 资源加载失败 {dllName}");
                    _isCompleted = true;
                    return;
                }

                var dllData = dataHandle.GetRawFileData();
                if (dllData == null || dllData.Length == 0)
                {
                    _contextRef.LogError($"xCore: LState 获取Dll数据失败 {dllName}");
                    _isCompleted = true;
                    return;
                }
                
                CheckAlreadyLoadedAssembly("HotUpdate");
                
                Assembly assembly = Assembly.Load(dllData);
                _contextRef.LogInfo($"xCore: LState 加载热更新Dll:{dllName}");
                CallAssemblyStaticMethod(assembly, "HotUpdate.DemoStatic", "DemoStart");
            }

            _isSuccess = true;
            _nextStateID = "LS_SetDefaultPackage";
            _isCompleted = true;
        }

        private static void CheckAlreadyLoadedAssembly(string assemblyName)
        {
#if !UNITY_EDITOR
            var alreadyLoaded = AppDomain.CurrentDomain.GetAssemblies()
                .Any(a => string.Equals(a.GetName().Name, assemblyName, StringComparison.Ordinal));
            if (alreadyLoaded)
            {
                Debug.LogError($"CheckAlreadyLoadedAssembly alreadyLoaded {assemblyName}");
            }
#endif                
        }

        public void CallAssemblyStaticMethod(Assembly assembly, string typeName, string methodName)
        {
            var type = assembly.GetType(typeName);
            if (type == null)
            {
                Debug.LogError($"CallAssemblyStaticMethod assembly.GetType({typeName}) == null");
                return;
            }
            var methodInfo = type.GetMethod(methodName);
            if (methodInfo == null)
            {
                Debug.LogError($"CallAssemblyStaticMethod methodInfo == null, typeName={typeName}, methodName={methodName}");
                return;
            }
            var param = new object[methodInfo.GetParameters().Length];
            methodInfo.Invoke(null, param);
        }
    }
}
