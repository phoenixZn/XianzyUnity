using System;
using UnityEngine;

namespace Xease.CoreGame
{
    public class ViewWrapperBase : IViewWrapper
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

        public bool IsReady => LoadState == ViewLoadState.Ready;

        public ViewLoadState LoadState { get; private set; } = ViewLoadState.None;

        public void RequestLoad(string assetLocation)
        {
            _assetLocation = assetLocation;
        }

        public string AssetLocation => _assetLocation;

        public void SetLoadState(ViewLoadState loadState)
        {
            LoadState = loadState;
        }

        public void BindProxy(IViewTransformProxy proxy)
        {
            if (_disposed)
                return;

            _proxy?.Dispose();
            _proxy = proxy ?? NullViewTransformProxy.Instance;
            FlushToProxy();
        }

        public void ApplyTransform(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            if (_disposed)
                return;

            _position = position;
            _rotation = rotation;
            _scale = scale;
            FlushToProxy();
        }

        public void SetActive(bool active)
        {
            if (_disposed)
                return;

            _active = active;
            FlushToProxy();
        }

        public void FlushToProxy()
        {
            if (_proxy == null || !_proxy.IsValid)
                return;

            _proxy.SetPosition(_position);
            _proxy.SetRotation(_rotation);
            _proxy.SetScale(_scale);
            _proxy.SetActive(_active);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _proxy?.Dispose();
            _proxy = NullViewTransformProxy.Instance;
            LoadState = ViewLoadState.None;
        }
    }
}
