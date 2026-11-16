namespace Xease.CoreGame
{
    // 属性通道。合成顺序与取整见 StatMath
    public enum StatChannel
    {
        // 与基础值相加，同通道求和
        Add = 0,

        // 加算倍率，同通道求和后乘一次
        Increase = 1,

        // 每条单独乘一次，按修饰符 Id 升序
        More = 2,

        // 替换通道合成结果；Priority 更高者胜，同优先级 Id 更大者胜
        Override = 3,
    }

    /// <summary>
    /// Increase 与 More 的刻度。通道里的一整倍对应该常量。
    /// </summary>
    public static class StatScale
    {
        // 一整倍。百分比按该刻度折算
        public const long Permyriad = 10000;
    }

    /// <summary>
    /// 属性合成。
    /// 无 Override 时：基础值与 Add 相加，乘上（一整倍 + Increase），再按修饰符 Id 升序逐条乘 More。
    /// 整除向零取整。Override 胜出后不再走上述通道。钳制由调用方在得到结果后执行。
    /// </summary>
    public static class StatMath
    {
        /// <summary>
        /// 计算 value * numerator / denominator，向零取整。
        /// 先拆成商和余数再乘，避免一次性乘积溢出；商与分子的乘积若仍溢出，结果不可用。
        /// 分母为 0、或被乘数、分子为 0 时返回 0。
        /// </summary>
        public static long Scale(long value, long numerator, long denominator)
        {
            if (denominator == 0 || value == 0 || numerator == 0)
                return 0;

            long quotient = value / denominator;
            long remainder = value % denominator;
            return quotient * numerator + remainder * numerator / denominator;
        }

        /// <summary>把基础值、Add 与 Increase 合成到 More 之前。</summary>
        public static long ApplyIncrease(long baseValue, long add, long increase)
        {
            return Scale(baseValue + add, StatScale.Permyriad + increase, StatScale.Permyriad);
        }

        /// <summary>乘上一条 More。more 为 0 时数值不变。</summary>
        public static long ApplyMore(long value, long more)
        {
            return Scale(value, StatScale.Permyriad + more, StatScale.Permyriad);
        }

        /// <summary>收到闭区间内。先不低于 min，再不高过 max。</summary>
        public static long Clamp(long value, long min, long max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
