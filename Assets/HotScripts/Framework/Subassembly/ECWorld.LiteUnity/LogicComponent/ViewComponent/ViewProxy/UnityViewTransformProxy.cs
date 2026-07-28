using UnityEngine;

namespace Xease.CoreGame
{
    public sealed class UnityViewTransformProxy : IViewTransformProxy
    {
        //这里对Transform、GameObject 只是聚合性质的引用，并不是拥有
        private Transform _transformRef;
        private GameObject _gameObjectRef;

        public UnityViewTransformProxy(Transform transformRef)
        {
            _transformRef = transformRef;
            _gameObjectRef = transformRef != null ? transformRef.gameObject : null;
        }

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

        public void Dispose()
        {
            // 只清理字段；GameObject 销毁/还池由 ViewWrapper 子类 ReleaseOwnedView 负责
            _gameObjectRef = null;
            _transformRef = null;
        }
    }
}
