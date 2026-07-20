using System;
using UnityEngine;

namespace Xease.CoreGame
{
    public class ViewWrapperBase : IViewWrapper, IViewTransformSyncable
    {
        private IViewTransformProxy _proxy;
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _scale = Vector3.one;
        private bool _active = true;
        private bool _disposed;

        public ViewWrapperBase(IViewTransformProxy proxy = null)
        {
            _proxy = proxy ?? NullViewTransformProxy.Instance;
        }

        protected bool IsDisposed => _disposed;

        public ViewLoadState LoadState { get; private set; } = ViewLoadState.None;

        public void SetLoadState(ViewLoadState loadState)
        {
            LoadState = loadState;
        }

        //////////////////////////////////////////////////////////////////////////
        // IViewWrapper
        public bool IsReady => LoadState == ViewLoadState.Ready;

        public void BindProxy(IViewTransformProxy proxy)
        {
            if (_disposed)
                return;

            // 只换绑句柄；资源所有权由子类在 Acquire / ReleaseOwnedView 中管理
            // （若此处调用 ReleaseOwnedView，会在「先赋值 _instance 再 BindProxy」时误毁新建对象）
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
            ReleaseOwnedView();
            _proxy?.Dispose();
            _proxy = NullViewTransformProxy.Instance;
            LoadState = ViewLoadState.None;
            SyncTransform = true;
        }

        /// <summary>
        /// 子类释放自己持有的表现资源（Destroy / 还池 / Detach）。
        /// </summary>
        protected virtual void ReleaseOwnedView()
        {
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
