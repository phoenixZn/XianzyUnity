using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// Unity Transform 聚合引用代理：不拥有 GameObject；实例经 SharedPool 租还，Dispose 清引用后归还。
    /// </summary>
    public sealed class UnityViewTransformProxy : IViewTransformProxy
    {
        // 聚合引用，不拥有 GameObject 生命周期
        private Transform _transformRef;
        private GameObject _gameObjectRef;

        /// <summary>
        /// SharedPool 工厂用无参构造。
        /// </summary>
        public UnityViewTransformProxy()
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        /// <summary>
        /// 从 SharedPool 租用并绑定 Transform 引用。
        /// </summary>
        public static UnityViewTransformProxy Rent(Transform transformRef)
        {
            var proxy = G.SharedPool.Rent<UnityViewTransformProxy>();
            proxy.Bind(transformRef);
            return proxy;
        }

        /// <summary>
        /// 写入 Transform / GameObject 聚合引用，不改变所有权。
        /// </summary>
        public void Bind(Transform transformRef)
        {
            _transformRef = transformRef;
            _gameObjectRef = transformRef != null ? transformRef.gameObject : null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IViewTransformProxy:
        public bool IsValid => _transformRef != null;

        public void SetPosition(Vector3 position)
        {
            if (_transformRef == null)
                return;
            _transformRef.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            if (_transformRef == null)
                return;
            _transformRef.rotation = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            if (_transformRef == null)
                return;
            _transformRef.localScale = scale;
        }

        public void SetActive(bool active)
        {
            if (_gameObjectRef == null)
                return;
            _gameObjectRef.SetActive(active);
        }

        /// <summary>
        /// 清空引用并归还 SharedPool；GameObject 销毁/还池由 ViewWrapper 子类 ReleaseOwnedView 负责。
        /// </summary>
        public void Dispose()
        {
            _gameObjectRef = null;
            _transformRef = null;
            G.SharedPool.Return(this);
        }
    }
}
