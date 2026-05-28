using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 请求资源版本状态
    /// </summary>
    public class LStateRequestPackageVersion : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("请求资源版本 !");
            _isCompleted = false;
            _nextStateID = null;
            UpdatePackageVersion().Forget();
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

        async UniTaskVoid UpdatePackageVersion()
        {
            var packageName = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageName);
            var package = YooAssets.GetPackage(packageName);
            var operation = package.RequestPackageVersionAsync();
            await operation;

            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.LogWarning(operation.Error);
                PatchEventDefine.PackageVersionRequestFailed.SendEventMessage();
            }
            else
            {
                Debug.Log($"Request package version : {operation.PackageVersion}");
                _contextRef.SetBlackboardValue(LSVKey.LSV_PackageVersion, operation.PackageVersion);
                _nextStateID = "LS_UpdatePackageManifest";
            }
            _isCompleted = true;
        }
    }
}
