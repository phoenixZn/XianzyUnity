namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 死亡：原地播放爆炸后挂 Death 组件，由 SysDeathProcess 销毁实体与 View。
    /// </summary>
    public partial class DemoStateDie : MainStateBase
    {
        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        public override void Destroy()
        {
            base.Destroy();
        }

        /// <summary>
        /// 进入即在当前位置播放死亡爆炸并请求销毁，避免停在 Die 残留 GO。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            if (_ownerEntity == null || _ownerEntity.hasComDeath)
                return;
            PlayDeathFx();
            _ownerEntity.AddComDeath(null);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 死亡爆炸；实现在 DemoStateDie.Unity.cs，命令行未编入时调用被抹掉
        partial void PlayDeathFx();
    }
}
