using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 表现包装基类：Transform 同步与代理换绑。Dispose 只释放表现；实例去向由 RecycleInstance 决定（默认交给 GC）。
    /// </summary>
    public class ViewWrapperBase : IViewWrapper, IViewTransformSyncable
    {
        private IViewTransformProxy _proxy;
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _scale = Vector3.one;
        private bool _active = true;
        private bool _disposed; // true=已释放；Reset 后可再次使用

        /// <summary>
        /// 默认空代理；真实 Transform 经 BindProxy 换绑。
        /// </summary>
        public ViewWrapperBase()
        {
            _proxy = NullViewTransformProxy.Instance;
        }

        protected bool IsDisposed => _disposed;

        //////////////////////////////////////////////////////////////////////////
        /// IViewWrapper:
        public virtual bool IsReady => true;

        public void SetActive(bool active)
        {
            if (_disposed)
                return;

            _active = active;
            FlushToProxy();
        }

        /// <summary>
        /// 释放持有的表现资源（GO / 加载句柄 / 代理）；随后 RecycleInstance 决定实例去向。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ReleaseOwnedAsset();
            _proxy?.Dispose();
            _proxy = NullViewTransformProxy.Instance;
            NeedsSyncTransform = true;
            OnDisposed();
            RecycleInstance();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IViewTransformSyncable:
        public bool NeedsSyncTransform { get; set; } = true;

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
        /// This:

        /// <summary>
        /// 复位到位姿默认值与未释放状态，供再次配置后使用（如从池中租出后）。
        /// </summary>
        public void Reset()
        {
            _disposed = false;
            _position = default;
            _rotation = Quaternion.identity;
            _scale = Vector3.one;
            _active = true;
            _proxy = NullViewTransformProxy.Instance;
            NeedsSyncTransform = true;
            OnReset();
        }

        public void BindProxy(IViewTransformProxy proxy)
        {
            if (_disposed)
                return;

            // 只换绑句柄；资源所有权由子类在 Acquire / ReleaseOwnedAsset 中管理
            // （若此处调用 ReleaseOwnedAsset，会在「先赋值 _instance 再 BindProxy」时误毁新建对象）
            _proxy?.Dispose();
            _proxy = proxy ?? NullViewTransformProxy.Instance;
            FlushToProxy();
        }

        // 子类释放自己持有的 Asset（Destroy / 还池 / Detach）
        protected virtual void ReleaseOwnedAsset()
        {
        }

        // Dispose 末尾钩子：子类复位自身获取状态等
        protected virtual void OnDisposed()
        {
        }

        // 释放表现之后的实例去向；默认空=非池化，交给 GC
        protected virtual void RecycleInstance()
        {
        }

        // 子类清自身加载/资源字段
        protected virtual void OnReset()
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// Proxy Flush
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
