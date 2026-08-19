namespace Xease.CoreGame
{
    /// <summary>
    /// 替代 LiteUnity 下 YooAssetView.Unity/AsyncAssetViewWrapper（依赖 YooAsset，已从本工程排除）；
    /// 供 ViewComponent.SetComView 默认包装器编译通过，不加载真实资源。
    /// </summary>
    public class AsyncAssetViewWrapper : ViewWrapperBase, IViewAcquirable
    {
        /// <summary>
        /// 纯 C# 工程不持有 AssetLocation 加载态。
        /// </summary>
        public ViewLoadState LoadState { get; private set; } = ViewLoadState.None;

        /// <summary>
        /// 始终 false，避免 SysViewLoader 走资源获取。
        /// </summary>
        public bool HasPendingAcquire => false;

        /// <summary>
        /// 与 Assets 侧相同的构造签名；location 在纯 C# 工程中忽略。
        /// </summary>
        public AsyncAssetViewWrapper(string assetLocation = null)
        {
        }

        /// <summary>
        /// 纯 C# 工程立即以失败结束，不访问 Asset 服务。
        /// </summary>
        public void BeginAcquire(ViewAcquireContext ctx)
        {
            ctx.Complete(false);
        }

        /// <summary>
        /// 记录加载态，供接口契约完整。
        /// </summary>
        public void SetLoadState(ViewLoadState state)
        {
            LoadState = state;
        }
    }
}
