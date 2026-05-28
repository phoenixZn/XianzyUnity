using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// 下载完成状态
    /// </summary>
    public class LStateDownloadPackageOver : LauncherState
    {
        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("资源文件下载完毕！");
        }

        public override void Leave()
        {
            base.Leave();
        }

        public override void Update(float dt)
        {
            base.Update(dt);
        }

        public override string CheckTransitions()
        {
            return "LS_ClearCacheBundle";
        }
    }
}
