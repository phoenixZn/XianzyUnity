using System;
using Unity.Collections;
using UnityEngine;

namespace LitMotion.Collections
{
    /// <summary>
    /// CLI 空曲线：不拷贝 AnimationCurve，Evaluate 恒等。禁止业务使用 Ease.CustomAnimationCurve。
    /// </summary>
    public struct UnsafeAnimationCurve : IDisposable
    {
        /// <summary>CLI 上始终视为未创建，避免走 Native 拷贝分支。</summary>
        public bool IsCreated => false;

        /// <summary>忽略源曲线与分配器。</summary>
        public UnsafeAnimationCurve(AnimationCurve animationCurve, AllocatorManager.AllocatorHandle allocator)
        {
        }

        /// <summary>忽略分配器。</summary>
        public UnsafeAnimationCurve(AllocatorManager.AllocatorHandle allocator)
        {
        }

        /// <summary>空拷贝。</summary>
        public void CopyFrom(AnimationCurve animationCurve)
        {
        }

        /// <summary>空拷贝。</summary>
        public void CopyFrom(in UnsafeAnimationCurve animationCurve)
        {
        }

        /// <summary>恒等求值（自定义曲线在 CLI 不受支持）。</summary>
        public float Evaluate(float time) => time;

        /// <summary>空释放。</summary>
        public void Dispose()
        {
        }
    }
}
