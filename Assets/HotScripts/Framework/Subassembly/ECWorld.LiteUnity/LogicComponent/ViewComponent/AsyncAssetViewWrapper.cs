using System;
using UnityEngine;
using Xease;
using YooAsset;
using Object = UnityEngine.Object;

namespace Xease.CoreGame
{
    /// <summary>
    /// 异步 Asset 加载策略：持有 Instantiate 出的 GameObject，释放时 Destroy。
    /// AssetLocation / RequestLoad 为本类具体 API，不进入 IViewAcquirable。
    /// </summary>
    public class AsyncAssetViewWrapper : ViewWrapperBase, IViewAcquirable
    {
        private string _assetLocation;
        private GameObject _instance;

        public AsyncAssetViewWrapper(string assetLocation = null)
        {
            if (!string.IsNullOrEmpty(assetLocation))
                RequestLoad(assetLocation);
        }

        //////////////////////////////////////////////////////////////////////////
        // Asset 配置（具体类 API）
        public string AssetLocation => _assetLocation;

        public void RequestLoad(string assetLocation)
        {
            _assetLocation = assetLocation;
            SetLoadState(ViewLoadState.None);
        }

        //////////////////////////////////////////////////////////////////////////
        // IViewAcquirable
        public bool HasPendingAcquire =>
            LoadState == ViewLoadState.None && !string.IsNullOrEmpty(_assetLocation);

        public void BeginAcquire(ViewAcquireContext ctx)
        {
            if (IsDisposed)
            {
                ctx.OnCompleted?.Invoke(false, null);
                return;
            }

            var assetSvc = GEnv.Inst?.Services?.AssetSvc;
            if (assetSvc == null)
            {
                ReleaseOwnedView();
                BindProxy(NullViewTransformProxy.Instance);
                ctx.OnCompleted?.Invoke(true, NullViewTransformProxy.Instance);
                return;
            }

            var location = _assetLocation;
            assetSvc.LoadAssetAsync<GameObject>(location, handle => OnAssetLoaded(location, handle, ctx));
        }

        private void OnAssetLoaded(string location, AssetHandle handle, ViewAcquireContext ctx)
        {
            if (IsDisposed)
            {
                ctx.OnCompleted?.Invoke(false, null);
                return;
            }

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                WLogger.LogError($"AsyncAssetViewWrapper load failed: {location}");
                ctx.OnCompleted?.Invoke(false, null);
                return;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                WLogger.LogError($"AsyncAssetViewWrapper asset is not GameObject: {location}");
                ctx.OnCompleted?.Invoke(false, null);
                return;
            }

            ReleaseOwnedView();
            _instance = Object.Instantiate(prefab);
            var proxy = new UnityViewTransformProxy(_instance.transform);
            BindProxy(proxy);
            ctx.OnCompleted?.Invoke(true, proxy);
        }

        protected override void ReleaseOwnedView()
        {
            if (_instance == null)
                return;

            Object.Destroy(_instance);
            _instance = null;
        }
    }
}
