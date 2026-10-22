using System;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace Xease.CoreGame
{
    public partial class PooledAssetViewWrapper
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // LoadAssetAsync 缓存回调，IsDone 时同步 Invoke 再 Rent；释放时 Return 到 GOPool
        // 句柄被 supersede 时只退订，不 Release（留 Loader 缓存）
        private AssetHandle _pendingHandle;

        // 仅当前 pending 的 Acquire 回调上下文
        private ViewAcquireContext _pendingCtx;

        // 实例级缓存，避免每次 BeginAcquire 分配闭包
        private Action<AssetHandle> _onPrefabLoaded;

        partial void InitHost()
        {
            _onPrefabLoaded = OnPrefabLoaded;
            LoadState = ViewLoadState.None;
        }

        partial void OnRebindAssetLocation()
        {
            CancelPendingAcquire(notifyFailure: false);
            SetLoadState(ViewLoadState.None);
        }

        // 认领后共享 BeginAcquire 不再回调；同步完成时回调可能在返回前发生
        partial void BeginAcquireHost(ViewAcquireContext ctx)
        {
            _acquireHandled = true;

            if (IsDisposed)
            {
                ctx.Complete(false);
                return;
            }

            if (G.Asset == null)
            {
                CancelPendingAcquire(notifyFailure: false);
                ReleaseOwnedAsset();
                BindProxy(NullViewTransformProxy.Instance);
                ctx.Complete(true);
                return;
            }

            if (G.GOPool == null)
            {
                WLogger.LogError($"PooledAssetViewWrapper pool missing: {_assetLocation}");
                CancelPendingAcquire(notifyFailure: false);
                ctx.Complete(false);
                return;
            }

            // last-wins：退订旧 handle，不回调旧 ctx
            CancelPendingAcquire(notifyFailure: false);
            _pendingCtx = ctx;
            // IsDone 时会在 return 前同步 Invoke 并已清 pending；仅异步未完成时挂上返回值
            var handle = G.Asset.LoadAssetAsync<GameObject>(_assetLocation, _onPrefabLoaded);
            if (handle == null)
            {
                WLogger.LogError($"PooledAssetViewWrapper load failed: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            if (!handle.IsDone)
                _pendingHandle = handle;
        }

        partial void ReleaseOwnedInstance()
        {
            if (_instance == null)
                return;

            if (G.GOPool != null)
                G.GOPool.Return(_instance);
            else
                Object.Destroy(_instance);

            _instance = null;
        }

        partial void OnDisposedHost()
        {
            CancelPendingAcquire(notifyFailure: true);
            LoadState = ViewLoadState.None;
        }

        partial void ResetHost()
        {
            _pendingHandle = null;
            _pendingCtx = default;
            LoadState = ViewLoadState.None;
        }

        // 预制体就绪后 Rent；陈旧回调丢弃。失败不租用，避免泄漏
        private void OnPrefabLoaded(AssetHandle handle)
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

            if (G.GOPool == null)
            {
                WLogger.LogError($"PooledAssetViewWrapper pool missing: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                WLogger.LogError($"PooledAssetViewWrapper load failed: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                WLogger.LogError($"PooledAssetViewWrapper asset is not GameObject: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            var instance = G.GOPool.Rent(prefab);
            if (instance == null)
            {
                WLogger.LogError($"PooledAssetViewWrapper rent failed: {_assetLocation}");
                InvokePendingCompleted(false);
                return;
            }

            ReleaseOwnedAsset();
            _instance = instance;
            BindProxy(UnityViewTransformProxy.Rent(_instance.transform, _assetLocation));
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
                _pendingHandle.Completed -= _onPrefabLoaded;

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
    }
}
