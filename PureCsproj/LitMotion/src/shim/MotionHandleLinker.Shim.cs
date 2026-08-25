using UnityEngine;

namespace LitMotion
{
    // CLI 占位：仅让 MotionHandleExtensions.AddTo(GameObject) 通过编译；命令行禁止调用 AddTo(go)
    internal sealed class MotionHandleLinker : MonoBehaviour
    {
        public void Register(MotionHandle handle, LinkBehavior linkBehaviour)
        {
        }
    }
}
