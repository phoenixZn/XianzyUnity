namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 死亡：挂 Death 组件，由 SysDeathProcess 销毁实体与 View。
    /// </summary>
    public class DemoStateDie : MainStateBase
    {
        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        public override void Destroy()
        {
            base.Destroy();
        }

        /// <summary>
        /// 进入即请求销毁，避免停在 Die 残留 GO。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            if (_ownerEntity == null || _ownerEntity.hasComDeath)
                return;
            _ownerEntity.AddComDeath(null);
        }
    }
}
