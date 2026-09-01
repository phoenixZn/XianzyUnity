using System.Threading;
#if !CONSOLE_CLIENT
using Cysharp.Threading.Tasks;
using Xease;
#endif

namespace Xease.CoreGame
{
    public partial class DemoModeLoading
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 预热完成才放行配置的 GST_InitGame
        private bool _prewarmDone;
        private CancellationTokenSource _prewarmCts;

        private void CancelPrewarm()
        {
            if (_prewarmCts == null)
                return;
            _prewarmCts.Cancel();
            _prewarmCts.Dispose();
            _prewarmCts = null;
        }

#if !CONSOLE_CLIENT
        private const int PrewarmCount = 20;

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
#else
        // 命令行无 GO 池，直接放行 Loading
        private void StartPrewarm()
        {
            _prewarmDone = true;
        }
#endif
    }
}
