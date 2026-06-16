using UnityEngine;

namespace Xease.CoreGame
{
    public sealed class UnityViewTransformProxy : IViewTransformProxy
    {
        private Transform _transform;
        private GameObject _gameObject;

        public UnityViewTransformProxy(Transform transform)
        {
            _transform = transform;
            _gameObject = transform != null ? transform.gameObject : null;
        }

        public bool IsValid => _transform != null;

        public void SetPosition(Vector3 position)
        {
            if (_transform == null)
                return;
            _transform.position = position;
        }

        public void SetRotation(Quaternion rotation)
        {
            if (_transform == null)
                return;
            _transform.rotation = rotation;
        }

        public void SetScale(Vector3 scale)
        {
            if (_transform == null)
                return;
            _transform.localScale = scale;
        }

        public void SetActive(bool active)
        {
            if (_gameObject == null)
                return;
            _gameObject.SetActive(active);
        }

        public void Dispose()
        {
            if (_gameObject != null)
            {
                Object.Destroy(_gameObject);
                _gameObject = null;
            }
            _transform = null;
        }
    }
}
