using UnityEngine;

namespace Xease.CoreGame
{
    public struct EvtGameLevelCompleted : IValueEvent
    {
    }

    //////////////////////////////////////////////////////////////////////////
    public class GameModePlaying : CustomBhvState
    {
        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            InnerClear();
        }
        
        public override void Destroy()
        {
            InnerClear();
            base.Destroy();
        }

        private void InnerClear()
        {
        }

        public override void Enter()
        {
            base.Enter();
        }
        
        public override float Update(float dt)
        {
            return base.Update(dt);
        }
        
        // 判断游戏胜利失败，要转到游戏结束状态
        public override string CheckTransitions()
        {
            var nextID = base.CheckTransitions();
            if (nextID != null)
            {
                if (this.isDebug())
                    this.Log($"GameModePlaying nextID={nextID}");
            }
            return nextID;
        }
    }
}
