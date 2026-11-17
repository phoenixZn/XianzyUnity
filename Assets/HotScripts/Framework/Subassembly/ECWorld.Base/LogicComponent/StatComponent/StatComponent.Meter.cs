using System;

namespace Xease.CoreGame
{
    // 当前值计量的上限变化策略。Meter 是有上限的当前读数，避免和对象池混淆
    public enum MeterResize : byte
    {
        // 只把当前值收到新区间内
        Clamp = 0,

        // 先跟上上限的增减，再收到新区间内
        AddDelta = 1,
    }

    public sealed partial class StatComponent
    {
        // 计量编号必须落在非负且小于该上界的区间
        private const int MeterIdLimit = 64;

        private MeterSlot[] _meters;

        // 已结算的上限、当前值或绑定方式变化时递增，不跟最终值版本混用
        private int _meterVersion;

        /// <summary>计量已结算的上限、当前值或绑定方式变化时递增，批次内只读不会更新。</summary>
        public int MeterVersion => _meterVersion;

        /// <summary>
        /// 把计量绑到某条属性的最终值上，作为上限。
        /// 当前值会被收到 [0, 上限]。上限为负时按 0。应先写好上限属性再绑定。
        /// </summary>
        public void BindMeter(int meterId, int maxStatId, long current, MeterResize resize)
        {
            if (!CheckStat(maxStatId))
                return;
            if (!PrepareMeter(meterId))
                return;

            long max = NonNegative(Get(maxStatId));
            ref var meter = ref _meters[meterId];
            meter.Used = true;
            meter.MaxStatId = maxStatId;
            meter.FixedMax = 0;
            meter.Resize = resize;
            meter.SeenMax = max;
            meter.Current = ClampCurrent(current, max);
            _meterVersion++;
        }

        /// <summary>绑定一个不随属性变化的固定上限。上限为负时按 0。</summary>
        public void BindFixedMeter(int meterId, long current, long max)
        {
            if (!PrepareMeter(meterId))
                return;
            if (max < 0)
                max = 0;

            ref var meter = ref _meters[meterId];
            meter.Used = true;
            meter.MaxStatId = -1;
            meter.FixedMax = max;
            meter.Resize = MeterResize.Clamp;
            meter.SeenMax = max;
            meter.Current = ClampCurrent(current, max);
            _meterVersion++;
        }

        /// <summary>已结算的当前值。批次内待提交的上限变化不影响本次读取，读取不会改计量状态。</summary>
        public long GetMeter(int meterId)
        {
            if (!IsMeter(meterId))
            {
                WLogger.LogError($"Stat.GetMeter 计量未绑定 {meterId}");
                return 0;
            }

            return _meters[meterId].Current;
        }

        /// <summary>已结算的上限。计量未绑定时返回 0。</summary>
        public long GetMeterMax(int meterId)
        {
            if (!IsMeter(meterId))
                return 0;

            return _meters[meterId].SeenMax;
        }

        /// <summary>
        /// 改当前值，返回实际加上的量（被区间截断后的差值）。
        /// 伤害传负值，治疗传正值。
        /// 批次内先按最新上限对齐绑定该属性的全部计量；返回量不含此次上限对齐造成的变化。
        /// </summary>
        public long ApplyMeterDelta(int meterId, long delta)
        {
            if (!IsMeter(meterId))
            {
                WLogger.LogError($"Stat.ApplyMeterDelta 计量未绑定 {meterId}");
                return 0;
            }

            // 扣血、治疗是显式结算点，不能依赖此前是否读取过上限属性
            int maxStatId = _meters[meterId].MaxStatId;
            if (maxStatId >= 0 && _deferred)
                SyncMetersForStat(maxStatId, Get(maxStatId));

            ref var meter = ref _meters[meterId];
            long before = meter.Current;
            meter.Current = ClampCurrent(AddSaturating(before, delta), meter.SeenMax);
            if (meter.Current != before)
                _meterVersion++;
            return meter.Current - before;
        }

        /// <summary>解除绑定。计量不存在时返回 false。</summary>
        public bool ClearMeter(int meterId)
        {
            if (!IsMeter(meterId))
                return false;

            _meters[meterId].Used = false;
            _meterVersion++;
            return true;
        }

        // 组件卸下时丢掉计量绑定，避免下一任实体沿用
        private void ResetMeters()
        {
            _meterVersion = 0;
            if (_meters == null)
                return;
            for (int i = 0; i < _meters.Length; i++)
                _meters[i].Used = false;
        }

        // 首次只按所需编号分配，之后按 2 倍扩容，不超过 MeterIdLimit
        private bool PrepareMeter(int meterId)
        {
            if ((uint)meterId >= MeterIdLimit)
            {
                WLogger.LogError($"Stat.BindMeter 计量编号非法 {meterId}");
                return false;
            }

            int needed = meterId + 1;
            if (_meters != null && _meters.Length >= needed)
                return true;

            int size = _meters == null ? needed : _meters.Length * 2;
            if (size < needed)
                size = needed;
            if (size > MeterIdLimit)
                size = MeterIdLimit;

            var next = new MeterSlot[size];
            if (_meters != null)
                Array.Copy(_meters, next, _meters.Length);
            _meters = next;
            return true;
        }

        private bool IsMeter(int meterId)
        {
            return _meters != null && (uint)meterId < (uint)_meters.Length && _meters[meterId].Used;
        }

        // 批次结束时只遍历计量存储一次，也处理此前已被 Get 刷新缓存的属性
        private void SyncMetersAfterChanges()
        {
            if (_meters == null)
                return;

            for (int i = 0; i < _meters.Length; i++)
            {
                ref var meter = ref _meters[i];
                if (!meter.Used || meter.MaxStatId < 0)
                    continue;
                ApplyMeterMax(ref meter, NonNegative(_final[meter.MaxStatId]));
            }
        }

        // 非批次属性写入或显式计量操作只结算绑定该属性的计量
        private void SyncMetersForStat(int statId, long max)
        {
            if (_meters == null)
                return;

            long cap = NonNegative(max);
            int count = _meters.Length;
            for (int i = 0; i < count; i++)
            {
                ref var meter = ref _meters[i];
                if (!meter.Used || meter.MaxStatId != statId)
                    continue;
                ApplyMeterMax(ref meter, cap);
            }
        }

        private void ApplyMeterMax(ref MeterSlot meter, long max)
        {
            if (meter.SeenMax != max)
            {
                if (meter.Resize == MeterResize.AddDelta)
                    meter.Current = AddSaturating(meter.Current, max - meter.SeenMax);
                meter.SeenMax = max;
                _meterVersion++;
            }

            long clamped = ClampCurrent(meter.Current, max);
            if (clamped == meter.Current)
                return;
            meter.Current = clamped;
            _meterVersion++;
        }

        private static long NonNegative(long value)
        {
            return value < 0 ? 0 : value;
        }

        private static long ClampCurrent(long current, long max)
        {
            if (current < 0)
                return 0;
            if (current > max)
                return max;
            return current;
        }

        // 在 long 边界饱和，避免当前值回绕
        private static long AddSaturating(long left, long right)
        {
            long sum = left + right;
            if (right >= 0 && sum < left)
                return long.MaxValue;
            if (right < 0 && sum > left)
                return long.MinValue;
            return sum;
        }

        private struct MeterSlot
        {
            public bool Used;

            // 负值表示使用 FixedMax，不读属性
            public int MaxStatId;

            public long FixedMax;
            public long Current;
            public long SeenMax;
            public MeterResize Resize;
        }
    }
}
