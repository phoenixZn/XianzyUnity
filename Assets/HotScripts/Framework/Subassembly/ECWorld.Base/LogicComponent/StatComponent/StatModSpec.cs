namespace Xease.CoreGame
{
    /// <summary>
    /// 一条属性修饰符。
    /// ConditionId 为 0 时进入最终值缓存；非 0 时只在 <see cref="StatComponent.Query{TContext}"/> 中按上下文生效。
    /// SourceId 由调用方定义。Buff 使用其实例 Id，因此手动修饰符用 0 或负数，避免和实例 Id 撞车。
    /// </summary>
    public readonly struct StatModSpec
    {
        // 稠密属性编号
        public readonly int StatId;

        // 合成通道
        public readonly StatChannel Channel;

        // 通道内的数量；倍率通道使用 StatScale
        public readonly long Value;

        // 仅 Override 比较胜出
        public readonly int Priority;

        // 0 表示无条件
        public readonly int ConditionId;

        // 0 表示不分组；RemoveModifiersBySource 按该值成批撤下
        public readonly int SourceId;

        /// <summary>按通道记下一条修饰符。未写的优先级、条件和来源都按“不参与”处理。</summary>
        public StatModSpec(int statId, StatChannel channel, long value, int priority = 0, int conditionId = 0, int sourceId = 0)
        {
            StatId = statId;
            Channel = channel;
            Value = value;
            Priority = priority;
            ConditionId = conditionId;
            SourceId = sourceId;
        }
    }
}
