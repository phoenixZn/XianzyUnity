using System;

namespace Xease.CoreGame
{
    /// <summary>
    /// 实体身上的 Buff。负责寿命、层数、充能和互斥；数值通过属性修饰符生效。
    /// 实时与回合共用 Advance：调用方决定 tick 的含义，并用 Phase 选择哪一次推进才扣减。
    /// 合并到已有实例时不改已挂上的修饰符。需要保留一段剩余时间时，用 <see cref="BuffTime.RefreshWithCarry"/> 算完再 <see cref="SetRemaining"/>。
    /// 动态修饰符可另挂到属性上，并把 SourceId 设为实例 Id，Buff 移除时一并撤下。
    /// </summary>
    public sealed class BuffComponent : LogicComponent
    {
        // 单个实体同时存在的 Buff 数量上界
        private const int BuffCapacityLimit = 512;

        // 全部 Buff 挂接的修饰符条数上界
        private const int LinkCapacityLimit = 4096;

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        /// <summary>卸下时按实例撤下仍挂着的属性修饰符。StatComponent 已卸下则不再访问它。</summary>
        public override void DisposeOnRemove()
        {
            var stats = HostStat();
            if (stats != null)
            {
                for (int i = 0; i < _count; i++)
                    stats.RemoveModifiersBySource(_buffs[i].InstanceId);
            }

            _count = 0;
            _linkCount = 0;
            _changeVersion = 0;
            base.DisposeOnRemove();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        private BuffRecord[] _buffs;
        private int _count;
        private ModLink[] _links;
        private int _linkCount;
        private int _nextInstanceId = 1;
        private int _changeVersion;

        /// <summary>当前 Buff 个数。GetAt 的合法索引小于该值，结构变化后索引失效。</summary>
        public int Count => _count;

        /// <summary>实例增删、层数、充能或持续时间被改写时递增。Advance 只扣剩余 tick 时不变。</summary>
        public int ChangeVersion => _changeVersion;

        /// <summary>
        /// 施加一个 Buff，返回实例 Id。失败返回 0。
        /// mods 为空时只作标记，不要求身上已有 StatComponent。
        /// </summary>
        public int Apply(in BuffSpec spec, BuffModSpec[] mods)
        {
            if (spec.TypeId <= 0)
            {
                WLogger.LogError("Buff.Apply 类型编号必须为正");
                return 0;
            }
            if (spec.Stacks <= 0)
            {
                WLogger.LogError("Buff.Apply 层数必须为正");
                return 0;
            }
            if (HasMods(mods) && HostStat() == null)
            {
                WLogger.LogError("Buff.Apply 有修饰符但缺少 StatComponent");
                return 0;
            }
            if (spec.GroupId != 0)
                return ApplyGrouped(spec, mods);
            return ApplyMerge(spec, mods);
        }

        /// <summary>按实例移除。不存在时返回 false。</summary>
        public bool Remove(int instanceId)
        {
            int index = FindIndexById(instanceId);
            if (index < 0)
                return false;
            RemoveAt(index);
            return true;
        }

        /// <summary>移除该 TypeId 的全部实例，返回个数。</summary>
        public int RemoveByType(int typeId)
        {
            return RemoveMatched(MatchKind.Type, typeId, 0);
        }

        /// <summary>移除该互斥组的全部实例，返回个数。groupId 为 0 时不移除。</summary>
        public int RemoveByGroup(int groupId)
        {
            if (groupId == 0)
                return 0;
            return RemoveMatched(MatchKind.Group, groupId, 0);
        }

        /// <summary>Tags 与掩码有任意公共置位则移除，返回个数。掩码为 0 时不移除。</summary>
        public int RemoveByTags(uint tags)
        {
            if (tags == 0)
                return 0;
            return RemoveMatched(MatchKind.Tags, 0, tags);
        }

        /// <summary>
        /// 推进某个相位的 tick。只影响 Phase 相等且不是永久的实例，剩余扣完则移除。
        /// 持续伤害等结算由调用方在 Advance 之前做，这样最后一跳仍看得到该 Buff。
        /// expiredIds 非空时写入被移除的实例 Id，写不下的只计数。
        /// </summary>
        public int Advance(byte phase, int ticks, int[] expiredIds)
        {
            if (ticks <= 0 || _count == 0)
                return 0;

            int expired = 0;
            int index = 0;
            while (index < _count)
            {
                if (_buffs[index].Phase != phase || _buffs[index].RemainingTicks < 0)
                {
                    index++;
                    continue;
                }

                int remain = _buffs[index].RemainingTicks - ticks;
                if (remain > 0)
                {
                    _buffs[index].RemainingTicks = remain;
                    index++;
                    continue;
                }

                int instanceId = _buffs[index].InstanceId;
                RemoveAt(index);
                if (expiredIds != null && expired < expiredIds.Length)
                    expiredIds[expired] = instanceId;
                expired++;
            }
            return expired;
        }

        /// <summary>
        /// 消耗充能。未使用充能或实例不存在时返回 false。
        /// 消耗后不大于 0 则移除该 Buff。
        /// </summary>
        public bool ConsumeCharge(int instanceId, int count)
        {
            if (count <= 0)
                return false;

            int index = FindIndexById(instanceId);
            if (index < 0)
                return false;

            int charges = _buffs[index].Charges;
            if (charges < 0)
                return false;
            if (count < charges)
            {
                _buffs[index].Charges = charges - count;
                _changeVersion++;
                return true;
            }

            RemoveAt(index);
            return true;
        }

        /// <summary>改层数。小于 1 时移除。超过上限时停在上限。</summary>
        public bool SetStacks(int instanceId, int stacks)
        {
            int index = FindIndexById(instanceId);
            if (index < 0)
                return false;
            if (stacks <= 0)
                return Remove(instanceId);

            int max = _buffs[index].MaxStacks;
            if (stacks > max)
                stacks = max;
            if (_buffs[index].Stacks == stacks)
                return true;

            _buffs[index].Stacks = stacks;
            WriteStackValues(instanceId, stacks);
            _changeVersion++;
            return true;
        }

        /// <summary>改剩余 tick。负值改为永久。实例不存在时返回 false。</summary>
        public bool SetRemaining(int instanceId, int ticks)
        {
            int index = FindIndexById(instanceId);
            if (index < 0)
                return false;
            if (_buffs[index].RemainingTicks == ticks)
                return true;

            _buffs[index].RemainingTicks = ticks;
            if (ticks < 0)
                _buffs[index].DurationTicks = -1;
            _changeVersion++;
            return true;
        }

        /// <summary>按实例取快照。</summary>
        public bool TryGet(int instanceId, out BuffView view)
        {
            int index = FindIndexById(instanceId);
            if (index < 0)
            {
                view = default;
                return false;
            }

            view = ToView(_buffs[index]);
            return true;
        }

        /// <summary>按当前稠密索引取快照。索引非法时返回默认快照并记错误。</summary>
        public BuffView GetAt(int index)
        {
            if ((uint)index >= (uint)_count)
            {
                WLogger.LogError($"Buff.GetAt 非法索引 {index}");
                return default;
            }
            return ToView(_buffs[index]);
        }

        /// <summary>该 TypeId 第一个实例的 Id。没有时返回 0。</summary>
        public int FindFirst(int typeId)
        {
            int index = FindIndexByType(typeId);
            if (index < 0)
                return 0;
            return _buffs[index].InstanceId;
        }

        private int ApplyMerge(in BuffSpec spec, BuffModSpec[] mods)
        {
            if (spec.Merge == BuffMerge.Independent)
                return Create(spec, mods);

            int index = FindIndexByType(spec.TypeId);
            switch (spec.Merge)
            {
                case BuffMerge.Reject:
                    if (index >= 0)
                        return 0;
                    return Create(spec, mods);
                case BuffMerge.Replace:
                {
                    if (index < 0)
                        return Create(spec, mods);
                    int replaced = Create(spec, mods);
                    if (replaced == 0)
                        return 0;
                    RemoveMatchedExcept(MatchKind.Type, spec.TypeId, replaced);
                    return replaced;
                }
                case BuffMerge.Refresh:
                    if (index < 0)
                        return Create(spec, mods);
                    WriteDuration(index, spec.DurationTicks);
                    return _buffs[index].InstanceId;
                case BuffMerge.StackRefresh:
                    if (index < 0)
                        return Create(spec, mods);
                    AddStacks(index, spec.Stacks, spec.MaxStacks);
                    WriteDuration(index, spec.DurationTicks);
                    return _buffs[index].InstanceId;
                case BuffMerge.KeepLonger:
                    if (index < 0)
                        return Create(spec, mods);
                    KeepLonger(index, spec.DurationTicks);
                    return _buffs[index].InstanceId;
                default:
                    WLogger.LogError($"Buff.Apply 未知合并 {spec.Merge}");
                    return 0;
            }
        }

        private int ApplyGrouped(in BuffSpec spec, BuffModSpec[] mods)
        {
            int best = -1;
            for (int i = 0; i < _count; i++)
            {
                if (_buffs[i].GroupId != spec.GroupId)
                    continue;
                if (best < 0 || _buffs[i].GroupPriority > _buffs[best].GroupPriority)
                    best = i;
            }

            if (best >= 0 && spec.GroupPriority < _buffs[best].GroupPriority)
                return 0;

            int created = Create(spec, mods);
            if (created == 0)
                return 0;
            if (best >= 0)
                RemoveMatchedExcept(MatchKind.Group, spec.GroupId, created);
            return created;
        }

        private int Create(in BuffSpec spec, BuffModSpec[] mods)
        {
            if (_count >= BuffCapacityLimit)
            {
                WLogger.LogError("Buff.Apply 已达到单个实体的 Buff 数量上界");
                return 0;
            }

            int stacks = spec.Stacks;
            int maxStacks = spec.MaxStacks > stacks ? spec.MaxStacks : stacks;
            int instanceId = NextInstanceId();
            if (!AttachMods(instanceId, stacks, mods))
                return 0;

            if (_buffs == null || _count == _buffs.Length)
                GrowBuffs();

            _buffs[_count] = new BuffRecord
            {
                InstanceId = instanceId,
                TypeId = spec.TypeId,
                Stacks = stacks,
                MaxStacks = maxStacks,
                RemainingTicks = spec.DurationTicks,
                DurationTicks = spec.DurationTicks,
                Charges = spec.Charges,
                Phase = spec.Phase,
                Tags = spec.Tags,
                GroupId = spec.GroupId,
                GroupPriority = spec.GroupPriority,
            };
            _count++;
            _changeVersion++;
            return instanceId;
        }

        private bool AttachMods(int instanceId, int stacks, BuffModSpec[] mods)
        {
            if (!HasMods(mods))
                return true;

            var stats = HostStat();
            if (stats == null)
            {
                WLogger.LogError("Buff.Apply 有修饰符但缺少 StatComponent");
                return false;
            }

            for (int i = 0; i < mods.Length; i++)
            {
                var spec = mods[i];
                long scaled = spec.ValuePerStack * stacks;
                int modId = stats.AddModifier(new StatModSpec(
                    spec.StatId,
                    spec.Channel,
                    scaled,
                    spec.Priority,
                    spec.ConditionId,
                    instanceId));
                if (modId == 0 || !AddLink(instanceId, modId, spec.ValuePerStack))
                {
                    stats.RemoveModifiersBySource(instanceId);
                    RemoveLinks(instanceId);
                    return false;
                }
            }
            return true;
        }

        private void AddStacks(int index, int incomingStacks, int incomingMax)
        {
            if (incomingMax > _buffs[index].MaxStacks)
                _buffs[index].MaxStacks = incomingMax;

            int max = _buffs[index].MaxStacks;
            long sum = (long)_buffs[index].Stacks + incomingStacks;
            if (sum > max)
                sum = max;
            if (sum > int.MaxValue)
                sum = int.MaxValue;
            int stacks = (int)sum;
            if (_buffs[index].Stacks == stacks)
                return;

            _buffs[index].Stacks = stacks;
            WriteStackValues(_buffs[index].InstanceId, stacks);
            _changeVersion++;
        }

        private void WriteDuration(int index, int durationTicks)
        {
            if (_buffs[index].DurationTicks == durationTicks && _buffs[index].RemainingTicks == durationTicks)
                return;
            _buffs[index].DurationTicks = durationTicks;
            _buffs[index].RemainingTicks = durationTicks;
            _changeVersion++;
        }

        private void KeepLonger(int index, int durationTicks)
        {
            int remain = _buffs[index].RemainingTicks;
            if (remain < 0 || durationTicks < 0)
            {
                if (remain < 0)
                    return;
                _buffs[index].RemainingTicks = -1;
                _buffs[index].DurationTicks = -1;
                _changeVersion++;
                return;
            }
            if (durationTicks <= remain)
                return;

            _buffs[index].RemainingTicks = durationTicks;
            _buffs[index].DurationTicks = durationTicks;
            _changeVersion++;
        }

        private void WriteStackValues(int instanceId, int stacks)
        {
            var stats = HostStat();
            if (stats == null)
                return;

            for (int i = 0; i < _linkCount; i++)
            {
                if (_links[i].InstanceId != instanceId)
                    continue;
                stats.SetModifierValue(_links[i].ModId, _links[i].PerStack * stacks);
            }
        }

        private void RemoveAt(int index)
        {
            int instanceId = _buffs[index].InstanceId;
            var stats = HostStat();
            if (stats != null)
                stats.RemoveModifiersBySource(instanceId);
            RemoveLinks(instanceId);

            int last = _count - 1;
            if (index != last)
                _buffs[index] = _buffs[last];
            _count = last;
            _changeVersion++;
        }

        private void RemoveMatchedExcept(MatchKind kind, int key, int keepId)
        {
            int index = 0;
            while (index < _count)
            {
                if (_buffs[index].InstanceId == keepId || !IsMatch(_buffs[index], kind, key, 0))
                {
                    index++;
                    continue;
                }

                RemoveAt(index);
            }
        }

        private int RemoveMatched(MatchKind kind, int key, uint tags)
        {
            int removed = 0;
            int index = 0;
            while (index < _count)
            {
                if (!IsMatch(_buffs[index], kind, key, tags))
                {
                    index++;
                    continue;
                }

                RemoveAt(index);
                removed++;
            }
            return removed;
        }

        private static bool IsMatch(in BuffRecord record, MatchKind kind, int key, uint tags)
        {
            switch (kind)
            {
                case MatchKind.Type:
                    return record.TypeId == key;
                case MatchKind.Group:
                    return record.GroupId == key;
                case MatchKind.Tags:
                    return (record.Tags & tags) != 0;
                default:
                    return false;
            }
        }

        private bool AddLink(int instanceId, int modId, long perStack)
        {
            if (_linkCount >= LinkCapacityLimit)
            {
                WLogger.LogError("Buff.Apply 已达到修饰符挂接数量上界");
                return false;
            }
            if (_links == null || _linkCount == _links.Length)
                GrowLinks();

            _links[_linkCount] = new ModLink
            {
                InstanceId = instanceId,
                ModId = modId,
                PerStack = perStack,
            };
            _linkCount++;
            return true;
        }

        private void RemoveLinks(int instanceId)
        {
            int index = 0;
            while (index < _linkCount)
            {
                if (_links[index].InstanceId != instanceId)
                {
                    index++;
                    continue;
                }

                int last = _linkCount - 1;
                if (index != last)
                    _links[index] = _links[last];
                _linkCount--;
            }
        }

        private int FindIndexById(int instanceId)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_buffs[i].InstanceId == instanceId)
                    return i;
            }
            return -1;
        }

        private int FindIndexByType(int typeId)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_buffs[i].TypeId == typeId)
                    return i;
            }
            return -1;
        }

        private BuffView ToView(in BuffRecord record)
        {
            return new BuffView(
                record.InstanceId,
                record.TypeId,
                record.Stacks,
                record.MaxStacks,
                record.RemainingTicks,
                record.DurationTicks,
                record.Charges,
                record.Phase,
                record.Tags,
                record.GroupId,
                record.GroupPriority);
        }

        private StatComponent HostStat()
        {
            var host = _hostEntity;
            if (host == null || !host.hasComStat)
                return null;
            return host.comStat;
        }

        private int NextInstanceId()
        {
            int id = _nextInstanceId;
            _nextInstanceId = _nextInstanceId == int.MaxValue ? 1 : _nextInstanceId + 1;
            return id;
        }

        private void GrowBuffs()
        {
            int size = _buffs == null ? 4 : _buffs.Length * 2;
            if (size > BuffCapacityLimit)
                size = BuffCapacityLimit;
            var next = new BuffRecord[size];
            if (_buffs != null)
                Array.Copy(_buffs, next, _count);
            _buffs = next;
        }

        private void GrowLinks()
        {
            int size = _links == null ? 4 : _links.Length * 2;
            if (size > LinkCapacityLimit)
                size = LinkCapacityLimit;
            var next = new ModLink[size];
            if (_links != null)
                Array.Copy(_links, next, _linkCount);
            _links = next;
        }

        private static bool HasMods(BuffModSpec[] mods)
        {
            return mods != null && mods.Length > 0;
        }

        private enum MatchKind : byte
        {
            Type = 0,
            Group = 1,
            Tags = 2,
        }

        private struct BuffRecord
        {
            public int InstanceId;
            public int TypeId;
            public int Stacks;
            public int MaxStacks;
            public int RemainingTicks;
            public int DurationTicks;
            public int Charges;
            public byte Phase;
            public uint Tags;
            public int GroupId;
            public int GroupPriority;
        }

        private struct ModLink
        {
            public int InstanceId;
            public int ModId;
            public long PerStack;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public BuffComponent comBuff
        {
            get { return (BuffComponent)GetComponent(LogicComponentsLookup.ComBuff); }
        }

        public bool hasComBuff
        {
            get { return HasComponent(LogicComponentsLookup.ComBuff); }
        }

        public void AddComBuff()
        {
            var index = LogicComponentsLookup.ComBuff;
            if (index < 0)
            {
                WLogger.LogError("AddComBuff 未初始化的组件索引");
                return;
            }

            var component = (BuffComponent)CreateComponent(index, typeof(BuffComponent));
            AddComponent(index, component);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComBuffIndex = new(typeof(BuffComponent));
        public static int ComBuff => _ComBuffIndex.Index;
    }
}
