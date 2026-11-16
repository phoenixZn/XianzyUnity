namespace Xease.CoreGame
{
    // 同 TypeId 再次施加时怎么处理。GroupId 非 0 时不走这里，改为按组优先级取舍
    public enum BuffMerge : byte
    {
        // 已有同 TypeId：重置持续时间。没有则新建
        Refresh = 0,

        // 已有同 TypeId：层数相加并受上限约束，同时重置持续时间
        StackRefresh = 1,

        // 已有同 TypeId：剩余时间取更长的一方。任一侧永久则变为永久
        KeepLonger = 2,

        // 总是新建
        Independent = 3,

        // 已有同 TypeId 则放弃本次
        Reject = 4,

        // 去掉同 TypeId 的全部实例后再新建
        Replace = 5,
    }

    /// <summary>
    /// 一次施加需要的数据。
    /// 持续时间为负表示 Advance 不结束它。充能为负表示不使用充能。
    /// GroupId 非 0 时只做组内取舍：新优先级不低于组内最高者则替换整组，否则拒绝。
    /// </summary>
    public readonly struct BuffSpec
    {
        // 正数。合并规则按它识别“同一种”
        public readonly int TypeId;

        // 负值表示不随 Advance 结束
        public readonly int DurationTicks;

        // 只有 Advance 传入相同相位才扣剩余 tick
        public readonly byte Phase;

        // 本次贡献的层数，必须为正
        public readonly int Stacks;

        // 小于本次层数时，上限会被抬到本次层数
        public readonly int MaxStacks;

        public readonly BuffMerge Merge;

        // 负值表示不使用充能；0 表示下次消耗即移除
        public readonly int Charges;

        // 与 RemoveByTags 按位相交即命中
        public readonly uint Tags;

        // 0 表示不参与互斥组
        public readonly int GroupId;

        // 同组内更高者留下；相等则后来者替换
        public readonly int GroupPriority;

        /// <summary>按字段组装。轻量施加可用 Marker、Timed、Stacked。</summary>
        public BuffSpec(
            int typeId,
            int durationTicks,
            byte phase,
            int stacks,
            int maxStacks,
            BuffMerge merge,
            int charges,
            uint tags,
            int groupId,
            int groupPriority)
        {
            TypeId = typeId;
            DurationTicks = durationTicks;
            Phase = phase;
            Stacks = stacks;
            MaxStacks = maxStacks;
            Merge = merge;
            Charges = charges;
            Tags = tags;
            GroupId = groupId;
            GroupPriority = groupPriority;
        }

        /// <summary>永久标记。再次施加同 TypeId 会被拒绝。</summary>
        public static BuffSpec Marker(int typeId)
        {
            return new BuffSpec(typeId, -1, 0, 1, 1, BuffMerge.Reject, -1, 0, 0, 0);
        }

        /// <summary>持续若干 tick。相位取默认值，再次施加会重置持续时间。</summary>
        public static BuffSpec Timed(int typeId, int durationTicks)
        {
            return new BuffSpec(typeId, durationTicks, 0, 1, 1, BuffMerge.Refresh, -1, 0, 0, 0);
        }

        /// <summary>可叠层，再次施加时加层并重置持续时间。</summary>
        public static BuffSpec Stacked(int typeId, int durationTicks, int stacks, int maxStacks)
        {
            return new BuffSpec(typeId, durationTicks, 0, stacks, maxStacks, BuffMerge.StackRefresh, -1, 0, 0, 0);
        }
    }

    /// <summary>
    /// Buff 挂到属性上的一条修饰符。层数变化时按 ValuePerStack 乘出新数量。
    /// </summary>
    public readonly struct BuffModSpec
    {
        public readonly int StatId;
        public readonly StatChannel Channel;

        // 每一层贡献的数量
        public readonly long ValuePerStack;

        public readonly int Priority;

        // 0 表示无条件，进入属性缓存
        public readonly int ConditionId;

        /// <summary>按层贡献一条属性修饰符。</summary>
        public BuffModSpec(int statId, StatChannel channel, long valuePerStack, int priority = 0, int conditionId = 0)
        {
            StatId = statId;
            Channel = channel;
            ValuePerStack = valuePerStack;
            Priority = priority;
            ConditionId = conditionId;
        }
    }

    /// <summary>
    /// Buff 的只读快照。
    /// </summary>
    public readonly struct BuffView
    {
        public readonly int InstanceId;
        public readonly int TypeId;
        public readonly int Stacks;
        public readonly int MaxStacks;

        // 负值表示 Advance 不结束它
        public readonly int RemainingTicks;

        public readonly int DurationTicks;

        // 负值表示不使用充能
        public readonly int Charges;

        public readonly byte Phase;
        public readonly uint Tags;
        public readonly int GroupId;
        public readonly int GroupPriority;

        /// <summary>剩余 tick 为负，Advance 不会因为时间结束它。</summary>
        public bool IsPermanent => RemainingTicks < 0;

        /// <summary>按字段组装一份快照。</summary>
        public BuffView(
            int instanceId,
            int typeId,
            int stacks,
            int maxStacks,
            int remainingTicks,
            int durationTicks,
            int charges,
            byte phase,
            uint tags,
            int groupId,
            int groupPriority)
        {
            InstanceId = instanceId;
            TypeId = typeId;
            Stacks = stacks;
            MaxStacks = maxStacks;
            RemainingTicks = remainingTicks;
            DurationTicks = durationTicks;
            Charges = charges;
            Phase = phase;
            Tags = tags;
            GroupId = groupId;
            GroupPriority = groupPriority;
        }
    }
}
