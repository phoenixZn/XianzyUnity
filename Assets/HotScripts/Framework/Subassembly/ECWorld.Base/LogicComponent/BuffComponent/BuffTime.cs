namespace Xease.CoreGame
{
    /// <summary>
    /// 与时钟无关的持续时间运算。Buff 组件不内置具体刷新规则，调用方算完再交给 <see cref="BuffComponent.SetRemaining"/>。
    /// </summary>
    public static class BuffTime
    {
        /// <summary>
        /// 刷新时把剩余 tick 里不超过 carryCap 的部分加到新持续时间上。
        /// 任一侧为永久（负值）时结果仍是永久。相加超出 int 上界时停在上界。
        /// </summary>
        public static int RefreshWithCarry(int remainingTicks, int newDurationTicks, int carryCap)
        {
            if (remainingTicks < 0 || newDurationTicks < 0)
                return -1;
            if (carryCap < 0)
                carryCap = 0;

            int carry = remainingTicks < carryCap ? remainingTicks : carryCap;
            long sum = (long)newDurationTicks + carry;
            if (sum > int.MaxValue)
                return int.MaxValue;
            return (int)sum;
        }
    }
}
