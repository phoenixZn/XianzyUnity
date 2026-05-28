using UnityEngine;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 初始化YooAsset资源系统状态
    /// </summary>
    public class LStateInitYooAsset : LauncherState
    {
        private bool _isCompleted = false;
        private string _nextStateID = null;

        public override void Enter()
        {
            base.Enter();
            PatchEventDefine.PatchStepsChange.SendEventMessage("初始化资源系统！");
            _isCompleted = false;
            _nextStateID = null;
            InitYooAsset();
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

        private void InitYooAsset()
        {
            // 初始化资源系统
            YooAssets.Initialize();
            _nextStateID = "LS_InitPackage";
            _isCompleted = true;
        }
    }
}
