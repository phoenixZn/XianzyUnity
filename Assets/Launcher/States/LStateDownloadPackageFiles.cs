using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 下载资源文件状态
    /// </summary>
    public class LStateDownloadPackageFiles : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("开始下载资源文件！");
            _isCompleted = false;
            _nextStateID = null;
            BeginDownload().Forget();
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

        async UniTaskVoid BeginDownload()
        {
            var downloader = (ResourceDownloaderOperation)_contextRef.GetBlackboardValue(LSVKey.LSV_Downloader);
            downloader.DownloadErrorCallback = PatchEventDefine.WebFileDownloadFailed.SendEventMessage;
            downloader.DownloadUpdateCallback = PatchEventDefine.DownloadUpdate.SendEventMessage;
            downloader.BeginDownload();
            await downloader;

            // 检测下载结果
            if (downloader.Status != EOperationStatus.Succeed)
                return;

            _nextStateID = "LS_DownloadPackageOver";
            _isCompleted = true;
        }
    }
}
