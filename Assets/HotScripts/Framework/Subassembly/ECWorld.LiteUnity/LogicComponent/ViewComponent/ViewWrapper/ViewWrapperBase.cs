using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 表现包装基类：Transform 同步与代理换绑。实例经 SharedPool 按具体类型租还；Dispose 释放表现资源后归还，而非一次性销毁。
    /// </summary>
    public abstract class ViewWrapperBase : IViewWrapper, IViewTransformSyncable
    {
        private IViewTransformProxy _proxy;
        private Vector3 _position;
        private Quaternion _rotation = Quaternion.identity;
        private Vector3 _scale = Vector3.one;
        private bool _active = true;
        private bool _disposed; // true=已释放或在池中；PrepareFromPool 复位后可复用

        // 默认空代理；真实 Transform 经 BindProxy 换绑
        protected ViewWrapperBase()
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
        /// 释放持有的表现资源（GO / 加载句柄 / 代理），再按具体类型归还 SharedPool。
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            ReleaseOwnedView();
            _proxy?.Dispose();
            _proxy = NullViewTransformProxy.Instance;
            NeedsSyncTransform = true;
            OnDisposed();
            ReturnToPool();
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
        /// 从 SharedPool 租用指定 Wrapper 并复位 Dispose 标记，供重新配置后挂到 ViewComponent。
        /// </summary>
        public static T RentFromPool<T>() where T : ViewWrapperBase, new()
        {
            var wrapper = G.SharedPool.Rent<T>();
            wrapper.PrepareFromPool();
            return wrapper;
        }

        /// <summary>
        /// 还池后再租出时复位基类状态；须在 Rent 之后、重新配置之前调用。
        /// </summary>
        public void PrepareFromPool()
        {
            _disposed = false;
            _position = default;
            _rotation = Quaternion.identity;
            _scale = Vector3.one;
            _active = true;
            _proxy = NullViewTransformProxy.Instance;
            NeedsSyncTransform = true;
            OnPrepareFromPool();
        }

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

        // 子类释放自己持有的表现资源（Destroy / 还池 / Detach）
        protected virtual void ReleaseOwnedView()
        {
        }

        // Dispose 末尾钩子：子类复位自身获取状态等
        protected virtual void OnDisposed()
        {
        }

        // 按具体类型归还 SharedPool，避免还进基类池
        protected abstract void ReturnToPool();

        // 子类清自身加载/资源字段
        protected virtual void OnPrepareFromPool()
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
