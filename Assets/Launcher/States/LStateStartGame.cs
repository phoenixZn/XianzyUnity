using UnityEngine;

namespace Launcher
{
    /// <summary>
    /// 启动游戏状态 - 切换到主页面场景
    /// </summary>
    public class LStateStartGame : LauncherState
    {
        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("启动游戏！");
            
            // 切换到主页面场景
            SceneEventDefine.ChangeToHomeScene.SendEventMessage();
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
            return _stateID; // 保持当前状态，不再转换
        }
    }
}
