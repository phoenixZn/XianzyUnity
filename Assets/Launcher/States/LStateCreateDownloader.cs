using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 创建下载器状态
    /// </summary>
    public class LStateCreateDownloader : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("创建资源下载器！");
            _isCompleted = false;
            _nextStateID = null;
            CreateDownloader().Forget();
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

        async UniTaskVoid CreateDownloader()
        {
            var packageName = (string)_contextRef.GetBlackboardValue(LSVKey.LSV_PackageName);
            var package = YooAssets.GetPackage(packageName);
            int downloadingMaxNum = 10;
            int failedTryAgain = 3;
            var downloader = package.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            _contextRef.SetBlackboardValue(LSVKey.LSV_Downloader, downloader);

            if (downloader.TotalDownloadCount == 0)
            {
                Debug.Log("Not found any download files !");
                _nextStateID = "LS_EndPatch";
                _isCompleted = true;
            }
            else
            {
                // 发现新更新文件后，挂起流程系统
                // 注意：开发者需要在下载前检测磁盘空间不足
                int totalDownloadCount = downloader.TotalDownloadCount;
                long totalDownloadBytes = downloader.TotalDownloadBytes;
                PatchEventDefine.FoundUpdateFiles.SendEventMessage(totalDownloadCount, totalDownloadBytes);
                // 等待用户确认后，会通过事件切换到下载状态（LS_DownloadPackageFiles）
                // 此时不设置 _nextStateID，保持当前状态等待事件触发
                _isCompleted = true;
            }
        }
    }
}
