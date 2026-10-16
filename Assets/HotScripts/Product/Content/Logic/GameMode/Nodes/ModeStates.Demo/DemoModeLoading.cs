namespace Xease.CoreGame
{
    /// <summary>
    /// Demo Loading：预热加载资源，完成后再进入 InitGame。
    /// </summary>
    public partial class DemoModeLoading : CustomBhvState
    {
        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

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

        /// <summary>
        /// 进入后异步预热资源。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            StartPrewarm();
        }

        /// <summary>
        /// 离开 Loading 时取消未完成的预热，避免销毁后回调。
        /// </summary>
        public override void Exit()
        {
            CancelPrewarm();
            base.Exit();
        }

        public override float Update(float dt)
        {
            return base.Update(dt);
        }

        /// <summary>
        /// 预热未完成时挡住缺省 NextState（GST_InitGame）。
        /// </summary>
        public override string CheckTransitions()
        {
            if (!_prewarmDone)
                return null;
            return base.CheckTransitions();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        private void InnerClear()
        {
            CancelPrewarm();
            _prewarmDone = false;
        }
    }
}
