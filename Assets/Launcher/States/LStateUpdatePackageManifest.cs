using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 更新资源清单状态
    /// </summary>
    public class LStateUpdatePackageManifest : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("更新资源清单！");
            _isCompleted = false;
            _nextStateID = null;
            UpdateManifest().Forget();
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

        async UniTaskVoid UpdateManifest()
        {
            var packageName = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageName);
            var packageVersion = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageVersion);
            var package = YooAssets.GetPackage(packageName);
            var operation = package.UpdatePackageManifestAsync(packageVersion);
            await operation;

            if (operation.Status != EOperationStatus.Succeed)
            {
                Debug.LogWarning(operation.Error);
                PatchEventDefine.PackageManifestUpdateFailed.SendEventMessage();
            }
            else
            {
                _nextStateID = "LS_CreateDownloader";
            }
            _isCompleted = true;
        }
    }
}
