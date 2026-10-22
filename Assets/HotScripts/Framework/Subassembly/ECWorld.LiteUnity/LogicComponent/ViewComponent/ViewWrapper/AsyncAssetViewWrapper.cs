using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 按 AssetLocation 异步实例化预制体的 ViewWrapper；释放时 Destroy。
    /// 加载与实例销毁在 AsyncAssetViewWrapper.Unity.cs。命令行未编入该文件时 partial void 被抹掉，不进入 SysViewLoader。
    /// </summary>
    public partial class AsyncAssetViewWrapper : ViewWrapperBase, IViewAcquirable, IViewAssetLocatable, IViewGameObjectHolder
    {
        // 资源定位地址；未配置时不进入加载
        private string _assetLocation;

        // 已实例化的表现；命令行保持空
        private GameObject _instance;

        // BeginAcquireHost 认领本次回调；未认领时由 BeginAcquire 失败结束
        private bool _acquireHandled;

        /// <summary>
        /// 无参构造，满足 RequestViewLoad 的 new() 约束。资源路径经 SetAssetLocation 配置。
        /// </summary>
        public AsyncAssetViewWrapper()
        {
            InitHost();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IViewAssetLocatable:

        /// <summary>
        /// 当前资源定位地址；未配置时为 null 或空。
        /// </summary>
        public string AssetLocation => _assetLocation;

        /// <summary>
        /// 写入待获取的资源定位地址。换地址时 Unity 分部丢弃进行中的加载。
        /// </summary>
        public void SetAssetLocation(string assetLocation)
        {
            OnRebindAssetLocation();
            _assetLocation = assetLocation;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IViewGameObjectHolder:

        /// <summary>
        /// 已就绪的实例；未加载或命令行宿主为空。
        /// </summary>
        public GameObject Instance => _instance;

        //////////////////////////////////////////////////////////////////////////
        /// IViewWrapper:

        /// <summary>
        /// 表现是否已经获取完成。
        /// </summary>
        public override bool IsReady => LoadState == ViewLoadState.Ready;

        //////////////////////////////////////////////////////////////////////////
        /// IViewAcquirable:

        /// <summary>
        /// 当前获取进度。命令行不改写时加载器不会挑选本实例；Unity 钩子改为 None 后才进入加载。
        /// </summary>
        public ViewLoadState LoadState { get; private set; } = ViewLoadState.Ready;

        /// <summary>
        /// 由 SysViewLoader 推进加载状态。
        /// </summary>
        public void SetLoadState(ViewLoadState loadState)
        {
            LoadState = loadState;
        }

        /// <summary>
        /// 已配置 location 且仍未开始加载时，SysViewLoader 会挑选本实例。
        /// </summary>
        public bool HasPendingAcquire =>
            LoadState == ViewLoadState.None && !string.IsNullOrEmpty(_assetLocation);

        /// <summary>
        /// 开始获取表现。Unity 分部认领后自行回调；未认领则失败结束。
        /// </summary>
        public void BeginAcquire(ViewAcquireContext ctx)
        {
            _acquireHandled = false;
            BeginAcquireHost(ctx);
            if (_acquireHandled)
                return;

            ctx.Complete(false);
        }

        //////////////////////////////////////////////////////////////////////////
        /// ViewWrapperBase：override

        // 实例去向在 ReleaseOwnedInstance；命令行无实现时调用被抹掉
        protected override void ReleaseOwnedAsset()
        {
            ReleaseOwnedInstance();
        }

        // 取消挂起加载；命令行无实现时调用被抹掉
        protected override void OnDisposed()
        {
            OnDisposedHost();
        }

        // 按本类型归还 SharedPool
        protected override void RecycleInstance()
        {
            G.SharedPool.Return(this);
        }

        // 清共享加载字段并回到命令行默认态；Unity 的 ResetHost 再改为 None
        protected override void OnReset()
        {
            _assetLocation = null;
            _instance = null;
            _acquireHandled = false;
            LoadState = ViewLoadState.Ready;
            ResetHost();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 缓存加载回调并把 LoadState 改为 None；未编入时调用被抹掉
        partial void InitHost();

        // 换 location 前丢弃进行中的加载并把 LoadState 改为 None；未编入时调用被抹掉
        partial void OnRebindAssetLocation();

        // 认领本次 Acquire 并加载资源；未编入时调用被抹掉，由 BeginAcquire 失败结束
        partial void BeginAcquireHost(ViewAcquireContext ctx);

        // 释放持有的实例；未编入时调用被抹掉
        partial void ReleaseOwnedInstance();

        // Dispose 时取消挂起加载；未编入时调用被抹掉
        partial void OnDisposedHost();

        // 清 Unity 侧句柄并把 LoadState 改为 None；未编入时调用被抹掉
        partial void ResetHost();
    }
}
