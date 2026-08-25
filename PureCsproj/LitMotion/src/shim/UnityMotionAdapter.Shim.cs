using Unity.Mathematics;
using UnityEngine;
using LitMotion;
using LitMotion.Adapters;

namespace LitMotion.Adapters
{
    /// <summary>
    /// Vector2 线性插值适配器。
    /// </summary>
    public readonly struct Vector2MotionAdapter : IMotionAdapter<Vector2, NoOptions>
    {
        /// <summary>
        /// 使用 Vector2.LerpUnclamped。
        /// </summary>
        public Vector2 Evaluate(ref Vector2 startValue, ref Vector2 endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return Vector2.LerpUnclamped(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// Vector3 线性插值适配器。
    /// </summary>
    public readonly struct Vector3MotionAdapter : IMotionAdapter<Vector3, NoOptions>
    {
        /// <summary>
        /// 使用 Vector3.LerpUnclamped。
        /// </summary>
        public Vector3 Evaluate(ref Vector3 startValue, ref Vector3 endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return Vector3.LerpUnclamped(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// Vector4 线性插值适配器。
    /// </summary>
    public readonly struct Vector4MotionAdapter : IMotionAdapter<Vector4, NoOptions>
    {
        /// <summary>
        /// 使用 Vector4.LerpUnclamped。
        /// </summary>
        public Vector4 Evaluate(ref Vector4 startValue, ref Vector4 endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return Vector4.LerpUnclamped(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// Quaternion 线性插值适配器。
    /// </summary>
    public readonly struct QuaternionMotionAdapter : IMotionAdapter<Quaternion, NoOptions>
    {
        /// <summary>
        /// 使用 Quaternion.LerpUnclamped。
        /// </summary>
        public Quaternion Evaluate(ref Quaternion startValue, ref Quaternion endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return Quaternion.LerpUnclamped(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// Color 线性插值适配器。
    /// </summary>
    public readonly struct ColorMotionAdapter : IMotionAdapter<Color, NoOptions>
    {
        /// <summary>
        /// 使用 Color.LerpUnclamped。
        /// </summary>
        public Color Evaluate(ref Color startValue, ref Color endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return Color.LerpUnclamped(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// Rect 分量线性插值适配器。
    /// </summary>
    public readonly struct RectMotionAdapter : IMotionAdapter<Rect, NoOptions>
    {
        /// <summary>
        /// 对 x/y/width/height 分别 lerp。
        /// </summary>
        public Rect Evaluate(ref Rect startValue, ref Rect endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            var x = math.lerp(startValue.x, endValue.x, context.Progress);
            var y = math.lerp(startValue.y, endValue.y, context.Progress);
            var width = math.lerp(startValue.width, endValue.width, context.Progress);
            var height = math.lerp(startValue.height, endValue.height, context.Progress);

            return new Rect(x, y, width, height);
        }
    }
}
