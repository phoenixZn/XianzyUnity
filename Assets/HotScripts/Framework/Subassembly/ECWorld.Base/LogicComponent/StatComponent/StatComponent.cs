using System;

namespace Xease.CoreGame
{
    /// <summary>
    /// 实体 Stat：基础值与修饰符，读取的是已经合成的最终值。
    /// 轻量用法只写基础值，或只使用 <see cref="StatChannel.Add"/>。
    /// Increase、More、Override、条件查询、钳制、当前值计量、写入批次都按需使用，互不强制。
    /// 伤害转换、命中结算、派生曲线不在这里计算：读最终值后由外部再写回。
    /// </summary>
    public sealed partial class StatComponent : LogicComponent
    {
        // 单个实体属性编号数量的上界
        private const int StatCapacityLimit = 4096;

        // 单个实体同时存在的修饰符数量上界
        private const int ModCapacityLimit = 4096;

        // 未单独钳制时的区间
        private const long UnboundedMin = long.MinValue;
        private const long UnboundedMax = long.MaxValue;

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        /// <summary>卸下时丢掉本实体的数值，数组容量留给组件池复用。</summary>
        public override void DisposeOnRemove()
        {
            _life++;
            _batch = 0;
            _deferred = false;
            ResetModifiers();
            ResetMeters();
            _statCount = 0;
            _changeVersion = 0;
            _useClamp = false;
            base.DisposeOnRemove();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 合法编号是 [0, _statCount)
        private int _statCount;

        // 基础值。与最终值分开，SetBase 不直接等于面板数值
        private long[] _base;

        // 无条件修饰符合成并钳制后的结果
        private long[] _final;

        // 每个属性上的无条件修饰符个数。为 0 时重算不再扫描
        private int[] _plainByStat;

        private int _conditionalCount;
        private int _modCount;
        private int _nextModId = 1;
        private Modifier[] _mods;

        // 最终值变化时递增。写入批次未结束时，被读到的属性会提前计入
        private int _changeVersion;

        // 每次卸下组件加一，使仍握着的 ChangeScope 失效
        private int _life;

        // 大于 0 时修改只记账，由 EndChanges 统一重算
        private int _batch;

        // 当前批次里出现过需要重算的修改
        private bool _deferred;

        private bool _useClamp;
        private long[] _clampMin;
        private long[] _clampMax;

        // Fold 时暂存 More，按修饰符 Id 排序后乘
        private int[] _moreIds;
        private long[] _moreValues;

        /// <summary>当前编号数量。合法编号是小于该值的非负整数。</summary>
        public int StatCount => _statCount;

        /// <summary>当前修饰符条数，含条件修饰符。</summary>
        public int ModifierCount => _modCount;

        /// <summary>最终值发生变化时递增，供外部决定要不要重算派生量。</summary>
        public int ChangeVersion => _changeVersion;

        /// <summary>
        /// 划定属性编号区间。不能缩小。
        /// 组件从池里复用时会清掉上一任在该区间内的基础值和最终值。
        /// </summary>
        public void EnsureStats(int statCount)
        {
            if (statCount <= 0 || statCount > StatCapacityLimit)
            {
                WLogger.LogError($"Stat.EnsureStats 数量非法 {statCount}");
                return;
            }
            if (_statCount > statCount)
            {
                WLogger.LogError("Stat.EnsureStats 不能缩小已有区间");
                return;
            }

            int oldCount = _statCount;
            _base = GrowLong(_base, statCount);
            _final = GrowLong(_final, statCount);
            _statCount = statCount;

            if (_plainByStat != null && _plainByStat.Length < statCount)
            {
                var counts = new int[statCount];
                Array.Copy(_plainByStat, counts, _plainByStat.Length);
                _plainByStat = counts;
            }
            if (_useClamp)
                EnsureClampStorage();

            for (int i = oldCount; i < statCount; i++)
            {
                _base[i] = 0;
                _final[i] = 0;
            }
        }

        /// <summary>写入基础值。无条件结果会随后重算；条件修饰符不进缓存。</summary>
        public void SetBase(int statId, long value)
        {
            if (!CheckStat(statId))
                return;
            if (_base[statId] == value)
                return;

            _base[statId] = value;
            ApplyTouch(statId);
        }

        /// <summary>基础值，不含修饰符和钳制。</summary>
        public long GetBase(int statId)
        {
            if (!CheckStat(statId))
                return 0;
            return _base[statId];
        }

        /// <summary>
        /// 无条件修饰符合成并钳制后的最终值。
        /// 写入批次尚未结束时，会先重算被读的这一条，其余属性仍等到 <see cref="EndChanges"/>。
        /// </summary>
        public long Get(int statId)
        {
            if (!CheckStat(statId))
                return 0;
            if (_deferred)
                Recalc(statId);
            return _final[statId];
        }

        /// <summary>
        /// 挂上一条修饰符并返回 Id。Id 从 1 递增，失败返回 0。
        /// 无条件修饰符会立刻重算该属性；在 <see cref="BeginChanges"/> 内则推迟到结束。
        /// </summary>
        public int AddModifier(in StatModSpec spec)
        {
            if (!CheckStat(spec.StatId))
                return 0;
            if (_modCount >= ModCapacityLimit)
            {
                WLogger.LogError("Stat.AddModifier 已达到单个实体的修饰符数量上界");
                return 0;
            }
            if (_mods == null || _modCount == _mods.Length)
                GrowMods();

            int id = NextModId();
            _mods[_modCount] = new Modifier
            {
                Id = id,
                StatId = spec.StatId,
                Channel = spec.Channel,
                Value = spec.Value,
                Priority = spec.Priority,
                ConditionId = spec.ConditionId,
                SourceId = spec.SourceId,
            };
            _modCount++;

            if (spec.ConditionId == 0)
            {
                EnsurePlainCounts();
                _plainByStat[spec.StatId]++;
                ApplyTouch(spec.StatId);
            }
            else
            {
                _conditionalCount++;
            }
            return id;
        }

        /// <summary>改数量。Id 不存在时返回 false。条件修饰符不改最终值缓存。</summary>
        public bool SetModifierValue(int modId, long value)
        {
            int index = IndexOfMod(modId);
            if (index < 0)
                return false;
            if (_mods[index].Value == value)
                return true;

            _mods[index].Value = value;
            if (_mods[index].ConditionId == 0)
                ApplyTouch(_mods[index].StatId);
            return true;
        }

        /// <summary>撤下一条修饰符。Id 不存在时返回 false。</summary>
        public bool RemoveModifier(int modId)
        {
            int index = IndexOfMod(modId);
            if (index < 0)
                return false;

            int statId = _mods[index].StatId;
            bool plain = _mods[index].ConditionId == 0;
            RemoveModAt(index);
            if (plain)
                ApplyTouch(statId);
            return true;
        }

        /// <summary>
        /// 撤下同一 SourceId 的全部修饰符，返回条数。
        /// SourceId 为 0 时撤下所有未分组的手动修饰符。
        /// </summary>
        public int RemoveModifiersBySource(int sourceId)
        {
            int removed = 0;
            int index = 0;
            while (index < _modCount)
            {
                if (_mods[index].SourceId != sourceId)
                {
                    index++;
                    continue;
                }

                int statId = _mods[index].StatId;
                bool plain = _mods[index].ConditionId == 0;
                RemoveModAt(index);
                removed++;
                if (plain)
                    ApplyTouch(statId);
            }
            return removed;
        }

        /// <summary>给该属性加闭区间钳制，并立刻参与合成。min 大于 max 时先执行下界。</summary>
        public void SetClamp(int statId, long min, long max)
        {
            if (!CheckStat(statId))
                return;

            EnsureClampStorage();
            _clampMin[statId] = min;
            _clampMax[statId] = max;
            ApplyTouch(statId);
        }

        /// <summary>去掉该属性的钳制。从未设置过钳制时无效果。</summary>
        public void ClearClamp(int statId)
        {
            if (!_useClamp || !CheckStat(statId))
                return;

            _clampMin[statId] = UnboundedMin;
            _clampMax[statId] = UnboundedMax;
            ApplyTouch(statId);
        }

        /// <summary>
        /// 开始一段连续写入。期间修改不逐条重算；<see cref="Get"/> 仍会重算被读到的属性。
        /// 必须配对 <see cref="EndChanges"/>，或改用 <see cref="BeginScope"/>。
        /// </summary>
        public void BeginChanges()
        {
            _batch++;
        }

        /// <summary>结束一段连续写入。最外层结束时重算全部最终值。</summary>
        public void EndChanges()
        {
            if (_batch <= 0)
            {
                WLogger.LogError("Stat.EndChanges 缺少 BeginChanges");
                return;
            }

            _batch--;
            if (_batch != 0 || !_deferred)
                return;

            _deferred = false;
            int count = _statCount;
            for (int i = 0; i < count; i++)
                Recalc(i);
        }

        /// <summary>BeginChanges / EndChanges 的范围。组件卸下后，这个范围不再改批次计数。</summary>
        public ChangeScope BeginScope()
        {
            return new ChangeScope(this, _life);
        }

        /// <summary>配对 BeginChanges 与 EndChanges。不要复制后分别 Dispose。</summary>
        public struct ChangeScope : IDisposable
        {
            private StatComponent _host;

            // 打开范围时的组件寿命，卸下后与宿主不一致
            private readonly int _life;

            internal ChangeScope(StatComponent host, int life)
            {
                _host = host;
                _life = life;
                host.BeginChanges();
            }

            /// <summary>结束对应的 BeginChanges。重复调用只有第一次生效。</summary>
            public void Dispose()
            {
                var host = _host;
                if (host == null)
                    return;

                _host = null;
                if (host._life != _life)
                    return;
                host.EndChanges();
            }
        }

        // 无条件结果：该属性没有无条件修饰符时直接取基础值
        private void Recalc(int statId)
        {
            long value = _plainByStat == null || _plainByStat[statId] <= 0
                ? ClampStat(statId, _base[statId])
                : Fold(statId, default(NoContext), false);
            WriteFinal(statId, value);
        }

        private void ApplyTouch(int statId)
        {
            if (_batch > 0)
            {
                _deferred = true;
                return;
            }
            Recalc(statId);
        }

        // includeConditional 为 false 时跳过带条件的修饰符，结果写入缓存
        private long Fold<TContext>(int statId, TContext context, bool includeConditional) where TContext : IStatContext
        {
            long add = 0;
            long increase = 0;
            int moreCount = 0;
            bool hasOverride = false;
            int bestPriority = 0;
            int bestId = 0;
            long bestValue = 0;

            int modCount = _modCount;
            for (int i = 0; i < modCount; i++)
            {
                ref var mod = ref _mods[i];
                if (mod.StatId != statId)
                    continue;
                if (mod.ConditionId != 0)
                {
                    if (!includeConditional || !context.Test(mod.ConditionId))
                        continue;
                }

                switch (mod.Channel)
                {
                    case StatChannel.Add:
                        add += mod.Value;
                        break;
                    case StatChannel.Increase:
                        increase += mod.Value;
                        break;
                    case StatChannel.More:
                        moreCount = PushMore(moreCount, mod.Id, mod.Value);
                        break;
                    case StatChannel.Override:
                        if (!hasOverride || mod.Priority > bestPriority || (mod.Priority == bestPriority && mod.Id > bestId))
                        {
                            hasOverride = true;
                            bestPriority = mod.Priority;
                            bestId = mod.Id;
                            bestValue = mod.Value;
                        }
                        break;
                    default:
                        WLogger.LogError($"Stat 未知通道 {mod.Channel}");
                        break;
                }
            }

            long value = hasOverride
                ? bestValue
                : ApplyMores(StatMath.ApplyIncrease(_base[statId], add, increase), moreCount);
            return ClampStat(statId, value);
        }

        private long ApplyMores(long value, int moreCount)
        {
            if (moreCount <= 1)
            {
                if (moreCount == 1)
                    value = StatMath.ApplyMore(value, _moreValues[0]);
                return value;
            }

            SortMores(moreCount);
            for (int i = 0; i < moreCount; i++)
                value = StatMath.ApplyMore(value, _moreValues[i]);
            return value;
        }

        private int PushMore(int count, int id, long value)
        {
            if (_moreIds == null || _moreIds.Length == count)
            {
                int size = count == 0 ? 4 : count * 2;
                Array.Resize(ref _moreIds, size);
                Array.Resize(ref _moreValues, size);
            }

            _moreIds[count] = id;
            _moreValues[count] = value;
            return count + 1;
        }

        private void SortMores(int count)
        {
            for (int i = 1; i < count; i++)
            {
                int id = _moreIds[i];
                long value = _moreValues[i];
                int j = i - 1;
                while (j >= 0 && _moreIds[j] > id)
                {
                    _moreIds[j + 1] = _moreIds[j];
                    _moreValues[j + 1] = _moreValues[j];
                    j--;
                }

                _moreIds[j + 1] = id;
                _moreValues[j + 1] = value;
            }
        }

        private void WriteFinal(int statId, long value)
        {
            if (_final[statId] == value)
                return;
            _final[statId] = value;
            _changeVersion++;
        }

        private long ClampStat(int statId, long value)
        {
            if (!_useClamp)
                return value;
            return StatMath.Clamp(value, _clampMin[statId], _clampMax[statId]);
        }

        private void EnsureClampStorage()
        {
            if (_clampMin == null || _clampMin.Length < _statCount)
            {
                var min = new long[_statCount];
                var max = new long[_statCount];
                for (int i = 0; i < _statCount; i++)
                {
                    min[i] = UnboundedMin;
                    max[i] = UnboundedMax;
                }
                if (_useClamp && _clampMin != null)
                {
                    Array.Copy(_clampMin, min, _clampMin.Length);
                    Array.Copy(_clampMax, max, _clampMax.Length);
                }

                _clampMin = min;
                _clampMax = max;
            }
            else if (!_useClamp)
            {
                for (int i = 0; i < _statCount; i++)
                {
                    _clampMin[i] = UnboundedMin;
                    _clampMax[i] = UnboundedMax;
                }
            }

            _useClamp = true;
        }

        private void EnsurePlainCounts()
        {
            if (_plainByStat != null && _plainByStat.Length >= _statCount)
                return;

            var counts = new int[_statCount];
            if (_plainByStat != null)
                Array.Copy(_plainByStat, counts, _plainByStat.Length);
            _plainByStat = counts;
        }

        private void RemoveModAt(int index)
        {
            ref var mod = ref _mods[index];
            if (mod.ConditionId == 0)
            {
                if (_plainByStat != null)
                    _plainByStat[mod.StatId]--;
            }
            else
            {
                _conditionalCount--;
            }

            int last = _modCount - 1;
            if (index != last)
                _mods[index] = _mods[last];
            _modCount = last;
        }

        private void ResetModifiers()
        {
            if (_plainByStat != null && _statCount > 0)
                Array.Clear(_plainByStat, 0, _statCount);
            _modCount = 0;
            _conditionalCount = 0;
        }

        private int IndexOfMod(int modId)
        {
            for (int i = 0; i < _modCount; i++)
            {
                if (_mods[i].Id == modId)
                    return i;
            }
            return -1;
        }

        private int NextModId()
        {
            int id = _nextModId;
            _nextModId = _nextModId == int.MaxValue ? 1 : _nextModId + 1;
            return id;
        }

        private void GrowMods()
        {
            int size = _mods == null ? 4 : _mods.Length * 2;
            if (size > ModCapacityLimit)
                size = ModCapacityLimit;
            var next = new Modifier[size];
            if (_mods != null)
                Array.Copy(_mods, next, _modCount);
            _mods = next;
        }

        private bool CheckStat(int statId)
        {
            if ((uint)statId < (uint)_statCount)
                return true;
            WLogger.LogError($"Stat 非法编号 {statId}");
            return false;
        }

        private static long[] GrowLong(long[] source, int needed)
        {
            if (source != null && source.Length >= needed)
                return source;
            int size = source == null ? needed : source.Length * 2;
            if (size < needed)
                size = needed;
            var next = new long[size];
            if (source != null)
                Array.Copy(source, next, source.Length);
            return next;
        }

        // 无条件折叠不需要上下文
        private struct NoContext : IStatContext
        {
            public bool Test(int conditionId)
            {
                return false;
            }
        }

        private struct Modifier
        {
            public int Id;
            public int StatId;
            public StatChannel Channel;
            public long Value;
            public int Priority;
            public int ConditionId;
            public int SourceId;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public StatComponent comStat
        {
            get { return (StatComponent)GetComponent(LogicComponentsLookup.ComStat); }
        }

        public bool hasComStat
        {
            get { return HasComponent(LogicComponentsLookup.ComStat); }
        }

        public void AddComStat(int statCount)
        {
            var index = LogicComponentsLookup.ComStat;
            if (index < 0)
            {
                WLogger.LogError("AddComStat 未初始化的组件索引");
                return;
            }

            var component = (StatComponent)CreateComponent(index, typeof(StatComponent));
            component.EnsureStats(statCount);
            AddComponent(index, component);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComStatIndex = new(typeof(StatComponent));
        public static int ComStat => _ComStatIndex.Index;
    }
}
