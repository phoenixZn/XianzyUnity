using UnityEngine;

namespace Xease.CoreGame
{
    public interface IViewTransformProxy
    {
        bool IsValid { get; }
        void SetPosition(Vector3 position);
        void SetRotation(Quaternion rotation);
        void SetScale(Vector3 scale);
        void SetActive(bool active);
        void Dispose();
    }
}
