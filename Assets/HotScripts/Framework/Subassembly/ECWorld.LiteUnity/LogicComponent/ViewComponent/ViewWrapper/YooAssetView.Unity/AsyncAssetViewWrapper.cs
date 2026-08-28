using System;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace Xease.CoreGame
{
    /// <summary>
    /// 异步Asset加载,ViewWrapper
    /// </summary>
    public class AsyncAssetViewWrapper : ViewWrapperBase, IViewAcquirable
    {
        /*
         异步 Asset 加载策略：持有 Instantiate 出的 GameObject，释放时 Destroy。
         AssetLocation / RequestLoad 为本类具体 API，不进入 IViewAcquirable。
         同实例连载时 last-wins：退订旧 handle 回调，仅当前 pending 落地并回调。
        */
        private string _assetLocation;
        private GameObject _instance;
        
        // 当前关心的加载句柄；被 supersede 时仅退订，不 Release（留 Loader 缓存）
        private AssetHandle _pendingHandle;
        
        // 仅当前 pending 的 Acquire 回调上下文
        private ViewAcquireContext _pendingCtx;
        
        // 实例级缓存，避免每次 BeginAcquire 分配闭包
        private readonly Action<AssetHandle> _onAssetLoaded;

        /// <summary>
        /// SharedPool 工厂用无参构造；资源路径经 SetAssetLocation 配置。
        /// </summary>
        public AsyncAssetViewWrapper()
        {
            _onAssetLoaded = OnAssetLoaded;
        }

        //////////////////////////////////////////////////////////////////////////
        // Asset 配置（具体类 API）
        public string AssetLocation => _assetLocation;

        // Instantiate 出的实例；未加载或已释放为 null
        public GameObject Instance => _instance;

        public void SetAssetLocation(string assetLocation)
        {
            // 换 location 时静默丢弃进行中的加载，避免旧资源落地
            CancelPendingAcquire(notifyFailure: false);
            _assetLocation = assetLocation;
            SetLoadState(ViewLoadState.None);
        }

        //////////////////////////////////////////////////////////////////////////
        /// IViewWrapper:
        public override bool IsReady => LoadState == ViewLoadState.Ready;

        //////////////////////////////////////////////////////////////////////////
        /// IViewAcquirable:
        public ViewLoadState LoadState { get; private set; } = ViewLoadState.None;

        public void SetLoadState(ViewLoadState loadState)
        {
            LoadState = loadState;
        }

        public bool HasPendingAcquire =>
            LoadState == ViewLoadState.None && !string.IsNullOrEmpty(_assetLocation);

        public void BeginAcquire(ViewAcquireContext ctx)
        {
            if (IsDisposed)
            {
                ctx.Complete(false);
                return;
            }

            var assetSvc = GEnv.Inst?.Services?.AssetSvc;
            if (assetSvc == null)
            {
                CancelPendingAcquire(notifyFailure: false);
                ReleaseOwnedView();
                BindProxy(NullViewTransformProxy.Instance);
                ctx.Complete(true);
                return;
            }

            // last-wins：退订旧 handle，不回调旧 ctx
            CancelPendingAcquire(notifyFailure: false);
            _pendingCtx = ctx;
            // IsDone 时会在 return 前同步 Invoke 并已清 pending；仅异步未完成时挂上返回值
            var handle = assetSvc.LoadAssetAsync<GameObject>(_assetLocation, _onAssetLoaded);
            if (handle != null && !handle.IsDone)
                _pendingHandle = handle;
        }

        private void OnAssetLoaded(AssetHandle handle)
        {
            // 陈旧回调（理论上已退订；防御）
            if (_pendingHandle != null && !ReferenceEquals(handle, _pendingHandle))
                return;

            // 同步完成竞态：Invoke 发生在赋值返回值之前，接受并挂上 pending
            if (_pendingHandle == null)
                _pendingHandle = handle;

            if (IsDisposed)
            {
                InvokePendingCompleted(false);
                return;
            }

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                WLogger.LogError($"AsyncAssetViewWrapper load failed: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                WLogger.LogError($"AsyncAssetViewWrapper asset is not GameObject: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            ReleaseOwnedView();
            _instance = Object.Instantiate(prefab);
            BindProxy(UnityViewTransformProxy.Rent(_instance.transform));
            InvokePendingCompleted(true);
        }

        // 回调当前 pending ctx 并清空挂起态（handle 已完成，无需 Completed -=）
        private void InvokePendingCompleted(bool success)
        {
            var ctx = _pendingCtx;
            _pendingHandle = null;
            _pendingCtx = default;
            ctx.Complete(success);
        }

        // 退订进行中的加载；notifyFailure 时回调旧 ctx 失败（Dispose）
        private void CancelPendingAcquire(bool notifyFailure)
        {
            if (_pendingHandle != null && !_pendingHandle.IsDone)
                _pendingHandle.Completed -= _onAssetLoaded;

            _pendingHandle = null;

            if (!notifyFailure)
            {
                _pendingCtx = default;
                return;
            }

            var ctx = _pendingCtx;
            _pendingCtx = default;
            ctx.Complete(false);
        }

        //////////////////////////////////////////////////////////////////////////
        /// ViewWrapperBase:
        protected override void ReleaseOwnedView()
        {
            if (_instance == null)
                return;

            Object.Destroy(_instance);
            _instance = null;
        }

        protected override void OnDisposed()
        {
            CancelPendingAcquire(notifyFailure: true);
            LoadState = ViewLoadState.None;
        }

        // 按本类型归还 SharedPool
        protected override void ReturnToPool()
        {
            G.SharedPool.Return(this);
        }

        // 还池后再租出时清加载态与实例引用
        protected override void OnPrepareFromPool()
        {
            _assetLocation = null;
            _instance = null;
            _pendingHandle = null;
            _pendingCtx = default;
            LoadState = ViewLoadState.None;
        }
    }
}
