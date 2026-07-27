using UnityEngine;

namespace Xease.CoreGame
{
    public sealed class NullViewTransformProxy : IViewTransformProxy
    {
        public static readonly NullViewTransformProxy Instance = new();

        public bool IsValid => true;

        public void SetPosition(Vector3 position) { }

        public void SetRotation(Quaternion rotation) { }

        public void SetScale(Vector3 scale) { }

        public void SetActive(bool active) { }

        public void Dispose() { }
    }
}
