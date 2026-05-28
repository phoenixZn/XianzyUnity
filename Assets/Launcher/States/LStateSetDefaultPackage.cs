using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 设置默认资源包状态
    /// </summary>
    public class LStateSetDefaultPackage : LauncherState
    {
        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("设置默认资源包！");
            
            var gamePackageName = _contextRef.GetBlackboardValue(LSVKey.LSV_GamePackageName, "PackDemoAsset");
            var gamePackage = YooAssets.GetPackage((string)gamePackageName);
            if (gamePackage != null)
            {
                YooAssets.SetDefaultPackage(gamePackage);
            }
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
            return "LS_StartGame";
        }
    }
}
