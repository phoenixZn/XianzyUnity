using Unity.Mathematics;
using LitMotion;
using LitMotion.Adapters;

namespace LitMotion.Adapters
{
    /// <summary>
    /// float 线性插值适配器（无 Job 注册）。
    /// </summary>
    public readonly struct FloatMotionAdapter : IMotionAdapter<float, NoOptions>
    {
        /// <summary>
        /// 按 context.Progress 在起止值之间 lerp。
        /// </summary>
        public float Evaluate(ref float startValue, ref float endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return math.lerp(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// double 线性插值适配器。
    /// </summary>
    public readonly struct DoubleMotionAdapter : IMotionAdapter<double, NoOptions>
    {
        /// <summary>
        /// 按 context.Progress 在起止值之间 lerp。
        /// </summary>
        public double Evaluate(ref double startValue, ref double endValue, ref NoOptions options, in MotionEvaluationContext context)
        {
            return math.lerp(startValue, endValue, context.Progress);
        }
    }

    /// <summary>
    /// int 线性插值适配器，按 IntegerOptions.RoundingMode 取整。
    /// </summary>
    public readonly struct IntMotionAdapter : IMotionAdapter<int, IntegerOptions>
    {
        /// <summary>
        /// lerp 后按 RoundingMode 转为 int。
        /// </summary>
        public int Evaluate(ref int startValue, ref int endValue, ref IntegerOptions options, in MotionEvaluationContext context)
        {
            var value = math.lerp(startValue, endValue, context.Progress);

            return options.RoundingMode switch
            {
                RoundingMode.AwayFromZero => value >= 0f ? (int)math.ceil(value) : (int)math.floor(value),
                RoundingMode.ToZero => (int)math.trunc(value),
                RoundingMode.ToPositiveInfinity => (int)math.ceil(value),
                RoundingMode.ToNegativeInfinity => (int)math.floor(value),
                _ => (int)math.round(value),
            };
        }
    }

    /// <summary>
    /// long 线性插值适配器，按 IntegerOptions.RoundingMode 取整。
    /// </summary>
    public readonly struct LongMotionAdapter : IMotionAdapter<long, IntegerOptions>
    {
        /// <summary>
        /// lerp 后按 RoundingMode 转为 long。
        /// </summary>
        public long Evaluate(ref long startValue, ref long endValue, ref IntegerOptions options, in MotionEvaluationContext context)
        {
            var value = math.lerp((double)startValue, endValue, context.Progress);

            return options.RoundingMode switch
            {
                RoundingMode.AwayFromZero => value >= 0f ? (long)math.ceil(value) : (long)math.floor(value),
                RoundingMode.ToZero => (long)math.trunc(value),
                RoundingMode.ToPositiveInfinity => (long)math.ceil(value),
                RoundingMode.ToNegativeInfinity => (long)math.floor(value),
                _ => (long)math.round(value),
            };
        }
    }
}
