using Entitas;

namespace Xease.CoreGame
{
    /// <summary>
    /// 属性与 Buff 的框架断言：通道合成、条件查询、写入批次、回合相位、充能、互斥和当前值计量。
    /// </summary>
    public class UnitTestSystem_StatBuff : ECWorldSystem, IInitializeSystem
    {
        const int Atk = 0;
        const int MaxHp = 1;
        const byte TurnEnd = 2;

        public UnitTestSystem_StatBuff(ECWorlds worlds) : base(worlds)
        {
        }

        public void Initialize()
        {
            WLogger.Log("UnitTest_StatBuff begin");
            TestScale();
            TestChannels();
            TestConditionalQuery();
            TestChangeScope();
            TestMeters();
            TestBuffLifetime();
            TestBuffStackMergeAndGroup();
            TestBuffCarryAndExternalSource();
            TestRecycle();
            WLogger.Log("UnitTest_StatBuff end");
        }

        void TestScale()
        {
            Check(0, StatMath.Scale(0, 15000, StatScale.Permyriad), "Scale zero");
            Check(-150, StatMath.Scale(-100, 15000, StatScale.Permyriad), "Scale negative");
            Check(150001, StatMath.Scale(100001, 15000, StatScale.Permyriad), "Scale split");
            Check(13, BuffTime.RefreshWithCarry(6, 10, 3), "RefreshWithCarry");
            Check(-1, BuffTime.RefreshWithCarry(-1, 10, 3), "RefreshWithCarry permanent");
        }

        void TestChannels()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            stats.SetBase(Atk, 100);

            int version = stats.ChangeVersion;
            stats.SetBase(Atk, 100);
            Check(version, stats.ChangeVersion, "SetBase same value");

            stats.SetBase(Atk, 10);
            Check(10, stats.Get(Atk), "base only");

            stats.AddModifier(new StatModSpec(Atk, StatChannel.Add, 20));
            stats.SetBase(Atk, 100);
            int increase = stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, 5000));
            Check(180, stats.Get(Atk), "add then increase");

            int more = stats.AddModifier(new StatModSpec(Atk, StatChannel.More, StatScale.Permyriad));
            Check(360, stats.Get(Atk), "one more");
            stats.RemoveModifier(more);
            Check(180, stats.Get(Atk), "remove more");
            stats.RemoveModifier(increase);
            stats.RemoveModifiersBySource(0);

            stats.SetBase(Atk, 10);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, 5000));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, 5000));
            Check(20, stats.Get(Atk), "two increases sum");
            stats.RemoveModifiersBySource(0);

            stats.AddModifier(new StatModSpec(Atk, StatChannel.More, 5000));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.More, 5000));
            Check(22, stats.Get(Atk), "two mores multiply");
            stats.RemoveModifiersBySource(0);

            stats.SetBase(Atk, 3);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.More, 3333));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.More, 5000));
            Check(4, stats.Get(Atk), "more order by id");
            stats.RemoveModifiersBySource(0);

            stats.SetBase(Atk, 100);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, -2500));
            Check(75, stats.Get(Atk), "negative increase");
            stats.RemoveModifiersBySource(0);

            stats.SetBase(Atk, 10);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Add, 5));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Override, 9, 0));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Override, 4, 1));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Override, 8, 1));
            Check(8, stats.Get(Atk), "override later id wins tie");
            stats.SetClamp(Atk, 0, 5);
            Check(5, stats.Get(Atk), "clamp after override");
            stats.ClearClamp(Atk);
            Check(8, stats.Get(Atk), "clear clamp");

            entity.Destroy();
        }

        void TestConditionalQuery()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            stats.SetBase(Atk, 10);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, 5000));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, 5000, conditionId: 3));

            Check(15, stats.Get(Atk), "conditional stays out of cache");
            Check(20, stats.Query(Atk, new Allow(3)), "conditional increase joins the sum");
            Check(15, stats.Query(Atk, new Allow(9)), "failed condition");

            stats.RemoveModifiersBySource(0);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Increase, StatScale.Permyriad));
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Add, 5, conditionId: 3));
            Check(20, stats.Get(Atk), "unconditional double");
            Check(30, stats.Query(Atk, new Allow(3)), "conditional add is inside the sum");

            entity.Destroy();
        }

        void TestChangeScope()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            stats.SetBase(Atk, 10);
            using (stats.BeginScope())
            {
                stats.AddModifier(new StatModSpec(Atk, StatChannel.Add, 5));
                stats.AddModifier(new StatModSpec(Atk, StatChannel.Add, 7));
                Check(22, stats.Get(Atk), "read inside change scope");
            }

            Check(22, stats.Get(Atk), "read after change scope");
            Check(2, stats.ModifierCount, "two modifiers");
            entity.Destroy();
        }

        void TestMeters()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            stats.SetBase(MaxHp, 100);
            stats.BindMeter(0, MaxHp, 100, MeterResize.Clamp);
            stats.BindMeter(1, MaxHp, 100, MeterResize.AddDelta);

            Check(-30, stats.ApplyMeterDelta(0, -30), "damage applied");
            Check(70, stats.GetMeter(0), "hp after damage");

            stats.SetBase(MaxHp, 150);
            Check(70, stats.GetMeter(0), "clamp meter ignores max growth");
            Check(150, stats.GetMeter(1), "add-delta meter follows max growth");

            stats.SetBase(MaxHp, 40);
            Check(40, stats.GetMeter(0), "clamp meter drops with max");
            Check(40, stats.GetMeter(1), "add-delta meter drops with max");

            stats.BindFixedMeter(2, 100, 40);
            Check(40, stats.GetMeter(2), "fixed meter caps current");
            Check(0, stats.ApplyMeterDelta(2, 10), "fixed meter rejects overflow heal");

            entity.Destroy();
        }

        void TestBuffLifetime()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            var buffs = entity.comBuff;
            stats.SetBase(Atk, 10);

            var turn = new BuffSpec(1, 1, TurnEnd, 1, 1, BuffMerge.Independent, -1, 0, 0, 0);
            int turnId = buffs.Apply(turn, Mods(Atk, 5));
            Check(15, stats.Get(Atk), "turn buff applied");
            Check(0, buffs.Advance(0, 5, null), "other phase does not expire");
            Check(15, stats.Get(Atk), "value remains on other phase");

            var expired = new int[2];
            Check(1, buffs.Advance(TurnEnd, 1, expired), "turn end expires");
            Check(turnId, expired[0], "expired id");
            Check(10, stats.Get(Atk), "turn buff removed");

            int forever = buffs.Apply(BuffSpec.Marker(2), Mods(Atk, 4));
            Check(0, buffs.Advance(0, 100, null), "permanent ignores advance");
            Check(true, buffs.TryGet(forever, out var foreverView) && foreverView.IsPermanent, "permanent view");
            Check(14, stats.Get(Atk), "permanent mod");

            var charged = new BuffSpec(3, -1, 0, 1, 1, BuffMerge.Independent, 2, 0, 0, 0);
            int chargeId = buffs.Apply(charged, Mods(Atk, 1));
            Check(true, buffs.ConsumeCharge(chargeId, 1), "consume one charge");
            Check(true, buffs.TryGet(chargeId, out var left) && left.Charges == 1, "one charge left");
            Check(true, buffs.ConsumeCharge(chargeId, 1), "consume last charge");
            Check(false, buffs.TryGet(chargeId, out _), "charges removed buff");
            Check(14, stats.Get(Atk), "charge mod removed");

            buffs.Remove(forever);
            entity.Destroy();
        }

        void TestBuffStackMergeAndGroup()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            var buffs = entity.comBuff;
            stats.SetBase(Atk, 10);

            int stacked = buffs.Apply(BuffSpec.Stacked(4, 3, 1, 3), Mods(Atk, 5));
            buffs.Apply(BuffSpec.Stacked(4, 3, 1, 3), Mods(Atk, 5));
            buffs.Apply(BuffSpec.Stacked(4, 3, 1, 3), Mods(Atk, 5));
            buffs.Apply(BuffSpec.Stacked(4, 3, 1, 3), Mods(Atk, 5));
            Check(true, buffs.TryGet(stacked, out var full) && full.Stacks == 3, "stack cap");
            Check(25, stats.Get(Atk), "stacked value");
            Check(3, full.RemainingTicks, "stack refresh resets duration");

            int marker = buffs.Apply(BuffSpec.Marker(8), null);
            Check(true, marker != 0, "marker applied");
            Check(0, buffs.Apply(BuffSpec.Marker(8), null), "reject existing marker");
            Check(marker, buffs.FindFirst(8), "marker id");

            var alone = new BuffSpec(9, -1, 0, 1, 1, BuffMerge.Independent, -1, 0, 0, 0);
            int first = buffs.Apply(alone, Mods(Atk, 5));
            int second = buffs.Apply(alone, Mods(Atk, 5));
            Check(true, first != 0 && second != 0 && first != second, "independent ids");
            Check(35, stats.Get(Atk), "independent values");
            buffs.Remove(first);
            Check(30, stats.Get(Atk), "remove one independent");

            var replaced = new BuffSpec(9, -1, 0, 1, 1, BuffMerge.Replace, -1, 0, 0, 0);
            buffs.Apply(replaced, Mods(Atk, 9));
            Check(1, CountType(buffs, 9), "replace leaves one");
            Check(34, stats.Get(Atk), "replace uses new mod");

            var low = new BuffSpec(11, -1, 0, 1, 1, BuffMerge.Independent, -1, 1u, 9, 1);
            var high = new BuffSpec(12, -1, 0, 1, 1, BuffMerge.Independent, -1, 2u, 9, 2);
            var lower = new BuffSpec(13, -1, 0, 1, 1, BuffMerge.Independent, -1, 4u, 9, 0);
            buffs.Apply(low, Mods(Atk, 5));
            Check(0, buffs.Apply(lower, Mods(Atk, 100)), "group rejects lower priority");
            buffs.Apply(high, Mods(Atk, 8));
            Check(0, buffs.FindFirst(11), "lower group member removed");
            Check(true, buffs.FindFirst(12) != 0, "higher group member remains");

            long beforeDispel = stats.Get(Atk);
            Check(1, buffs.RemoveByTags(2u), "dispel by tag");
            Check(true, stats.Get(Atk) < beforeDispel, "dispel drops the mod");

            entity.Destroy();
        }

        void TestBuffCarryAndExternalSource()
        {
            var entity = Spawn();
            var stats = entity.comStat;
            var buffs = entity.comBuff;
            stats.SetBase(Atk, 10);

            int timed = buffs.Apply(BuffSpec.Timed(20, 5), Mods(Atk, 2));
            buffs.Advance(0, 2, null);
            Check(true, buffs.TryGet(timed, out var mid) && mid.RemainingTicks == 3, "two ticks remain");
            buffs.Apply(BuffSpec.Timed(20, 5), null);
            Check(true, buffs.TryGet(timed, out mid) && mid.RemainingTicks == 5, "refresh resets duration");

            buffs.Advance(0, 2, null);
            Check(true, buffs.TryGet(timed, out mid), "still alive");
            int carried = BuffTime.RefreshWithCarry(mid.RemainingTicks, 5, 2);
            buffs.SetRemaining(timed, carried);
            Check(true, buffs.TryGet(timed, out mid) && mid.RemainingTicks == carried, "carry writes remaining");

            var longer = new BuffSpec(21, 2, 0, 1, 1, BuffMerge.KeepLonger, -1, 0, 0, 0);
            int keep = buffs.Apply(longer, null);
            buffs.Apply(new BuffSpec(21, 10, 0, 1, 1, BuffMerge.KeepLonger, -1, 0, 0, 0), null);
            Check(true, buffs.TryGet(keep, out var kept) && kept.RemainingTicks == 10, "keep longer");
            buffs.Apply(new BuffSpec(21, 3, 0, 1, 1, BuffMerge.KeepLonger, -1, 0, 0, 0), null);
            Check(true, buffs.TryGet(keep, out kept) && kept.RemainingTicks == 10, "shorter does not shrink");

            int source = buffs.Apply(BuffSpec.Marker(22), null);
            long beforeExternal = stats.Get(Atk);
            stats.AddModifier(new StatModSpec(Atk, StatChannel.Add, 6, sourceId: source));
            Check(beforeExternal + 6, stats.Get(Atk), "external mod uses buff id");
            buffs.Remove(source);
            Check(beforeExternal, stats.Get(Atk), "external mod leaves with buff");

            entity.Destroy();
        }

        void TestRecycle()
        {
            var entity = Spawn();
            entity.comStat.SetBase(Atk, 99);
            entity.comStat.AddModifier(new StatModSpec(Atk, StatChannel.Add, 1));
            entity.comBuff.Apply(BuffSpec.Marker(1), Mods(Atk, 4));
            entity.Destroy();

            var next = Spawn();
            Check(0, next.comStat.Get(Atk), "recycled base cleared");
            Check(0, next.comStat.ModifierCount, "recycled modifiers cleared");
            Check(0, next.comBuff.Count, "recycled buffs cleared");
            next.Destroy();
        }

        LogicEntity Spawn()
        {
            var entity = _logicWorld.CreateEntity();
            entity.AddComStat(4);
            entity.AddComBuff();
            return entity;
        }

        static BuffModSpec[] Mods(int statId, long perStack)
        {
            return new[] { new BuffModSpec(statId, StatChannel.Add, perStack) };
        }

        static int CountType(BuffComponent buffs, int typeId)
        {
            int count = 0;
            for (int i = 0; i < buffs.Count; i++)
            {
                if (buffs.GetAt(i).TypeId == typeId)
                    count++;
            }
            return count;
        }

        static void Check(long expected, long actual, string name)
        {
            if (expected != actual)
            {
                WLogger.LogError($"UnitTest FAIL: {name} expected={expected} actual={actual}");
                return;
            }

            WLogger.Log($"UnitTest PASS: {name}");
        }

        static void Check(bool expected, bool actual, string name)
        {
            if (expected != actual)
            {
                WLogger.LogError($"UnitTest FAIL: {name} expected={expected} actual={actual}");
                return;
            }

            WLogger.Log($"UnitTest PASS: {name}");
        }

        readonly struct Allow : IStatContext
        {
            readonly int _id;

            public Allow(int id)
            {
                _id = id;
            }

            public bool Test(int conditionId)
            {
                return conditionId == _id;
            }
        }
    }
}
