namespace Xease
{
    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 基于置换同余生成器（PCG）的随机数生成器：实现简单、速度快、统计性质好，且难以被简单预测。
    /// PCG 支持在相同种子下产生多条独立随机序列（不同「流」），便于游戏中一局一个总种子，
    /// 又为关卡生成、掉落等需要隔离上下文的子系统各用一流，避免一类随机影响另一类。
    ///
    /// https://www.pcg-random.org/
    /// 本实现参考最小 C 版本示例。https://github.com/imneme/pcg-c-basic
    /// </summary>
    public class RandomGeneratorPCG
    {
        // 64 位 LCG 状态；每步由此派生输出（PCG）。
        private ulong _state;

        // 将流编号编码为奇数增量，使各流为不同的子序列。
        private readonly ulong _inc;

        /// <summary>
        /// 随机数生成器的内部状态。
        /// </summary>
        public ulong State
        {
            get => _state;
            set
            {
                _state = value;
                PCG32(); // 前进一步，避免设置状态后立即取数时出现重复。
            }
        }

        /// <summary>
        /// 使用种子与流编号两部分初始化随机数生成器。
        /// </summary>
        /// <param name="state">状态初值（即种子）。</param>
        /// <param name="streamID">序列选择常数（即流编号），默认为 0。</param>
        public RandomGeneratorPCG(ulong state, ulong streamID = 0)
        {
            _state = 0ul;
            _inc = (streamID << 1) | 1ul; // PCG 要求增量为奇数（inc % 2 == 1）。
            // 标准 PCG 播种：先混合流，再混入用户种子，再混合一次。
            PCG32();
            _state += state;
            PCG32();
        }

        /// <summary>
        /// 生成一个均匀分布的随机数。
        /// </summary>
        /// <returns>均匀分布的 32 位无符号整数。</returns>
        private uint PCG32()
        {
            ulong oldState = _state;
            // LCG 递推：乘数为 PCG 公布的 64 位常数。
            _state = unchecked(_state * 6364136223846793005ul + _inc);
            // XSH RR 输出：异或/移位后，按旧状态高 5 位对 32 位结果做循环右移。
            uint xorshifted = (uint) (((oldState >> 18) ^ oldState) >> 27);
            int rot = (int) (oldState >> 59);
            // 无 uint32 循环右移指令时，用两次移位拼接实现。
            return (xorshifted >> rot) | (xorshifted << ((-rot) & 31));
        }

        /// <summary>
        /// 生成均匀分布的 r，满足 0 &lt;= r &lt; <paramref name="bound"/>。
        /// </summary>
        /// <param name="bound">上界（不包含）。</param>
        /// <returns>严格小于 <paramref name="bound"/> 的均匀分布 32 位无符号整数。</returns>
        private uint PCG32(uint bound)
        {
            if (bound <= 0)
            {
                bound = 1;
            }

            // 拒绝采样：丢弃 [0, threshold) 内的 r，使剩余区间长度为 bound 的整数倍，
            // 从而 r % bound 均匀（直接取模会产生偏向小余数的偏差）。
            uint threshold = ((uint) -bound) % bound; // 等价于 2^32 % bound

            // 均匀性保证循环必结束。实践中多数情况一次即中；若各 bound 等概率，
            // 平均约 82.25% 只需一次迭代。最坏如 bound = 2^31+1，会拒绝近一半输出；
            // 通常 bound 较小，被拒绝的比例很低。
            while (true)
            {
                uint r = PCG32();
                if (r >= threshold)
                    return r % bound;
            }
        }

        /// <summary>
        /// 生成区间 [0, <see cref="int.MaxValue"/>) 内的随机整数。
        /// </summary>
        /// <returns>非负随机整数。</returns>
        public int Next()
        {
            return Next(int.MaxValue);
        }

        /// <summary>
        /// 生成区间 [0, <paramref name="maxValue"/>) 内的随机整数。
        /// </summary>
        /// <param name="maxValue">上界（不包含）。</param>
        /// <returns>落在 [0, <paramref name="maxValue"/>) 内的随机整数。</returns>
        public int Next(int maxValue)
        {
            // 与常见 RNG 行为一致：非法上界时退化为仅可能取 0。
            if (maxValue < 0)
                maxValue = 1;

            return (int) PCG32((uint) maxValue);
        }

        /// <summary>
        /// 生成区间 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机整数。
        /// </summary>
        /// <param name="minValue">下界（包含）。</param>
        /// <param name="maxValue">上界（不包含）。</param>
        /// <returns>落在 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机整数。</returns>
        public int Next(int minValue, int maxValue)
        {
            if (maxValue < minValue)
                maxValue = minValue;

            // 用 long 计算跨度，避免 int 相减溢出（例如覆盖整个 int 范围时）。
            return (int) (minValue + PCG32((uint) ((long) maxValue - minValue)));
        }

        /// <summary>
        /// 生成区间 [0.0f, 1.0f) 内的随机单精度浮点数（近似均匀）。
        /// </summary>
        /// <returns>[0.0f, 1.0f) 内的随机浮点数。</returns>
        public float NextFloat()
        {
            // 先在 [0, bound) 取整数再缩放；避免 BitConverter 或原始 uint→float 技巧。
            int bound = int.MaxValue / 2 - 1;
            return Next(bound) * 1.0f / bound; // 近似 [0, 1) 上均匀
        }

        /// <summary>
        /// 生成区间 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机浮点数。
        /// </summary>
        /// <param name="minValue">下界（包含），可为负。</param>
        /// <param name="maxValue">上界（不包含）。</param>
        /// <returns>落在 [<paramref name="minValue"/>, <paramref name="maxValue"/>) 内的随机浮点数。</returns>
        public float NextFloat(float minValue, float maxValue)
        {
            if (maxValue < minValue)
                maxValue = minValue;

            return minValue + (maxValue - minValue) * NextFloat();
        }

        /// <summary>
        /// 生成随机布尔值。
        /// </summary>
        /// <returns>随机 true 或 false。</returns>
        public bool NextBool()
        {
            // 基于 [0,1) 浮点；因整数缩放，恰为 0.5 的概率极低。
            return NextFloat() <= 0.5f;
        }
    }
}