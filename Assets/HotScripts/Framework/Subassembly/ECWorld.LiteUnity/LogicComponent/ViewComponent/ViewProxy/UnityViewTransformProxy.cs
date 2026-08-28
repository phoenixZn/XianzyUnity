using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// Unity Transform 聚合引用代理：不拥有 GameObject。Rent 入池；new+Bind 不入池。Dispose 清引用，仅入池实例归还。
    /// </summary>
    public sealed class UnityViewTransformProxy : IViewTransformProxy
    {
        // 聚合引用，不拥有 GameObject 生命周期
        private Transform _transformRef;
        private GameObject _gameObjectRef;
        private bool _fromPool; // true=经 Rent 租出，Dispose 时还池

        /// <summary>
        /// 非池化构造；配合 Bind 使用。池化请走 Rent。
        /// </summary>
        public UnityViewTransformProxy()
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        /// <summary>
        /// 从 SharedPool 租用并绑定 Transform 引用；Dispose 时归还。
        /// </summary>
        public static UnityViewTransformProxy Rent(Transform transformRef)
        {
            var proxy = G.SharedPool.Rent<UnityViewTransformProxy>();
            proxy._fromPool = true;
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
        /// 清空引用；经 Rent 租出的实例归还 SharedPool。GameObject 销毁由 ViewWrapper 子类 ReleaseOwnedAsset 负责。
        /// </summary>
        public void Dispose()
        {
            _gameObjectRef = null;
            _transformRef = null;
            if (!_fromPool)
                return;

            _fromPool = false;
            G.SharedPool.Return(this);
        }
    }
}
