using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 替代 LiteUnity 下 ViewWrapper.Unity/AsyncAssetViewWrapper（依赖 YooAsset，已从本工程排除）；
    /// 供 ViewComponent.RequestViewLoad 泛型约束编译通过，不加载真实资源。
    /// </summary>
    public class AsyncAssetViewWrapper : ViewWrapperBase, IViewAcquirable, IViewAssetLocatable, IViewGameObjectHolder
    {
        /// <summary>
        /// 纯 C# 工程不持有 AssetLocation 加载态。
        /// </summary>
        public ViewLoadState LoadState { get; private set; } = ViewLoadState.Ready;

        /// <summary>
        /// 始终 false，避免 SysViewLoader 走资源获取。
        /// </summary>
        public bool HasPendingAcquire => false;

        /// <summary>
        /// 记录 location，纯 C# 工程不据此加载。
        /// </summary>
        public string AssetLocation { get; private set; }

        /// <summary>
        /// 纯 C# 工程无实例对象。
        /// </summary>
        public GameObject Instance => null;

        /// <summary>
        /// 无参构造；满足 RequestViewLoad 泛型 new() 约束。
        /// </summary>
        public AsyncAssetViewWrapper()
        {
        }

        /// <summary>
        /// 记录 location，不触发真实加载。
        /// </summary>
        public void SetAssetLocation(string assetLocation)
        {
            AssetLocation = assetLocation;
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

        // 按本类型归还 SharedPool
        protected override void RecycleInstance()
        {
            G.SharedPool.Return(this);
        }

        // Reset 时清 location
        protected override void OnReset()
        {
            AssetLocation = null;
        }
    }
}
