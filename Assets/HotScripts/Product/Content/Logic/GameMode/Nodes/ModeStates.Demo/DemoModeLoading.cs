using System.Threading;
using Cysharp.Threading.Tasks;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo Loading：经 GameObjectPool_Battle 预热 Born 所用 Cube/Sphere，完成后再进入 InitGame。
    /// </summary>
    public class DemoModeLoading : CustomBhvState
    {
        private const int PrewarmCount = 20;

        // 预热完成才放行配置的 GST_InitGame
        private bool _prewarmDone;
        private CancellationTokenSource _prewarmCts;

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
        /// 进入后异步预热 ActorCube / ActorSphere 各 20 个。
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

        // 无池则直接放行，避免卡在 Loading
        private void StartPrewarm()
        {
            CancelPrewarm();
            _prewarmDone = false;

            if (G.GameObjectPool_Battle == null)
            {
                this.LogError("DemoModeLoading GameObjectPool_Battle is null");
                _prewarmDone = true;
                return;
            }

            _prewarmCts = new CancellationTokenSource();
            PrewarmViewsAsync(_prewarmCts.Token).Forget();
        }

        // 并行预热 Cube/Sphere；取消不切状态，其它失败仍放行以免卡死
        private async UniTaskVoid PrewarmViewsAsync(CancellationToken ct)
        {
            try
            {
                var pool = G.GameObjectPool_Battle;
                if (pool == null)
                {
                    _prewarmDone = true;
                    return;
                }

                await UniTask.WhenAll(
                    pool.PrewarmAsync(DemoStateBorn.DemoViewAssetCube, PrewarmCount, ct),
                    pool.PrewarmAsync(DemoStateBorn.DemoViewAssetSphere, PrewarmCount, ct));
            }
            catch (System.OperationCanceledException)
            {
                return;
            }

            if (ct.IsCancellationRequested)
                return;

            _prewarmDone = true;
        }

        private void CancelPrewarm()
        {
            if (_prewarmCts == null)
                return;
            _prewarmCts.Cancel();
            _prewarmCts.Dispose();
            _prewarmCts = null;
        }
    }
}
