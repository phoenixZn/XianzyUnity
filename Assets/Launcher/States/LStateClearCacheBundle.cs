using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 清理缓存状态
    /// </summary>
    public class LStateClearCacheBundle : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("清理未使用的缓存文件！");
            _isCompleted = false;
            _nextStateID = null;
            var packageName = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageName);
            var package = YooAssets.GetPackage(packageName);
            var operation = package.ClearCacheFilesAsync(EFileClearMode.ClearUnusedBundleFiles);
            operation.Completed += Operation_Completed;
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

        private void Operation_Completed(YooAsset.AsyncOperationBase obj)
        {
            _nextStateID = "LS_EndPatch";
            _isCompleted = true;
        }
    }
}
