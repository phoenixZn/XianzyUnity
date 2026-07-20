using System;
using UnityEngine;

namespace Xease.CoreGame
{
    public class ViewWrapperBase : IViewWrapper, IAssetViewLoadable, IViewTransformSyncable
    {
        private IViewTransformProxy _proxy;
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _scale = Vector3.one;
        private bool _active = true;
        private string _assetLocation;
        private bool _disposed;

        public ViewWrapperBase(IViewTransformProxy proxy = null)
        {
            _proxy = proxy ?? NullViewTransformProxy.Instance;
        }

        //////////////////////////////////////////////////////////////////////////
        // IViewWrapper
        public bool IsReady => LoadState == ViewLoadState.Ready;

        public void BindProxy(IViewTransformProxy proxy)
        {
            if (_disposed)
                return;

            _proxy?.Dispose();
            _proxy = proxy ?? NullViewTransformProxy.Instance;
            FlushToProxy();
        }

        public void SetActive(bool active)
        {
            if (_disposed)
                return;

            _active = active;
            FlushToProxy();
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _proxy?.Dispose();
            _proxy = NullViewTransformProxy.Instance;
            _assetLocation = null;
            LoadState = ViewLoadState.None;
            SyncTransform = true;
        }

        //////////////////////////////////////////////////////////////////////////
        // IAssetViewLoadable
        public ViewLoadState LoadState { get; private set; } = ViewLoadState.None;

        public string AssetLocation => _assetLocation;

        public void RequestLoad(string assetLocation)
        {
            _assetLocation = assetLocation;
            LoadState = ViewLoadState.None;
        }

        public void SetLoadState(ViewLoadState loadState)
        {
            LoadState = loadState;
        }

        //////////////////////////////////////////////////////////////////////////
        // IViewTransformSyncable
        public bool SyncTransform { get; set; } = true;

        public void ApplyTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (_disposed)
                return;

            _position = position;
            _rotation = rotation;
            _scale = scale;
            FlushToProxy();
        }

        //////////////////////////////////////////////////////////////////////////
        // Proxy Flush
        public void FlushToProxy()
        {
            if (_proxy == null || !_proxy.IsValid)
                return;

            _proxy.SetPosition(_position);
            _proxy.SetRotation(_rotation);
            _proxy.SetScale(_scale);
            _proxy.SetActive(_active);
        }
    }
}
