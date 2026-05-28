using UnityEngine;
using UniFramework.Event;
using YooAsset;

namespace Launcher
{
    /// <summary>
    /// 初始化App状态 - 初始化游戏管理器、事件系统、加载更新页面
    /// </summary>
    public class LStateInitApp : LauncherState
    {
        public override void Enter()
        {
            base.Enter();
            
            PatchEventDefine.PatchStepsChange.SendEventMessage("初始化应用！");

            // 初始化事件系统
            UniEvent.Initalize();
            
            // 加载更新页面
            var go = Resources.Load<GameObject>("PatchWindow");
            if (go != null)
            {
                GameObject.Instantiate(go);
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
            // 检查是否需要加载运行时设置
            var playMode = (EPlayMode)_contextRef.GetBlackboardValue(LSVKey.LSV_PlayMode, EPlayMode.EditorSimulateMode);
            if (playMode == EPlayMode.HostPlayMode)
            {
                var runtimeSettings = _contextRef.GetBlackboardValue(LSVKey.LSV_HybridRuntimeSettings);
                if (runtimeSettings == null)
                {
                    return "LS_LoadHybridRuntimeSettings";
                }
            }
            
            return "LS_InitYooAsset";
        }
    }
}
