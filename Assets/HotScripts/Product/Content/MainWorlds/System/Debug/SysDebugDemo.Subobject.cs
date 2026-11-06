using System.Collections.Generic;
using UnityEngine;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugDemo
    {
        // 冒烟区域原点，远离演示单位；各用例沿 y 分道，互不落入对方的命中范围
        private static readonly Vector3 SmokeOrigin = new(0f, 0f, 100f);
        // 冒烟专用受击层，避免与业务目标互相命中
        private const uint SmokeLayer = 1u << 31;
        // 自开始起经过此时长后统一校验，秒；须长于各用例子物体的寿命
        private const float SmokeCheckDelay = 3.5f;
        // 穿透用例的目标数
        private const int SmokePierceTargetCount = 10;

        private SubobjectSmoke _subobjectSmoke; // 进行中的冒烟测试；null 表示未开始或已结束

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 子物体端到端冒烟：直线弹、穿透弹、纯实体子物体、实体与记录共存，以及来源链
        partial void SubobjectSmokeBegin()
        {
            var subobjects = _metaWorld.comUniSubobjects;
            if (subobjects == null)
            {
                G.LogError("SubobjectSmoke 缺少 UniSubobjectsComponent");
                return;
            }

            var smoke = new SubobjectSmoke
            {
                PreviousSink = subobjects.HitSink,
                CheckTime = subobjects.WorldTime + SmokeCheckDelay,
            };
            subobjects.HitSink = smoke;
            _subobjectSmoke = smoke;

            var root = CreateSmokeEntity(smoke, SmokeOrigin, EFaction.Friend, 0f);

            // 直线弹：先穿过友方不命中，再命中敌方一次后回收
            var lane = SmokeOrigin;
            smoke.LinearAlly = SubobjectEntityRef.Of(CreateSmokeEntity(smoke, lane + new Vector3(2.5f, 0f, 0f), EFaction.Friend, 0.5f));
            smoke.LinearEnemy = SubobjectEntityRef.Of(CreateSmokeEntity(smoke, lane + new Vector3(5f, 0f, 0f), EFaction.Enemy, 0.5f));
            var linear = SmokeArgs(root, lane, 20f, 2f);
            linear.Policy.MaxHitCount = 1;
            linear.Policy.DestroyWhenExhausted = true;
            smoke.LinearBullet = subobjects.SpawnRoutine(in linear);
            if (!smoke.LinearBullet.IsValid)
                smoke.Errors.Add("直线弹生成失败");

            // 穿透弹：不限总次数，每个目标只命中一次
            lane = SmokeOrigin + new Vector3(0f, 10f, 0f);
            for (var i = 0; i < SmokePierceTargetCount; i++)
                smoke.PierceTargets.Add(SubobjectEntityRef.Of(CreateSmokeEntity(smoke, lane + new Vector3(2f + i, 0f, 0f), EFaction.Enemy, 0.3f)));
            var pierce = SmokeArgs(root, lane, 20f, 2f);
            pierce.Policy.PerTargetMax = 1;
            subobjects.SpawnRoutine(in pierce);

            // 纯实体子物体：Logic 逐帧计数，寿命转为 ComLife
            var entityOnlyArgs = SmokeArgs(root, SmokeOrigin + new Vector3(0f, 20f, 0f), 0f, 0.5f);
            var entityOnly = subobjects.SpawnEntity(in entityOnlyArgs, new SubobjectEntityOptions { LogicConfigId = LogicConfig_Subobject.DemoSmokeLogicId });
            smoke.EntityOnly = SubobjectEntityRef.Of(entityOnly);
            if (entityOnly == null || entityOnly.comSubobject.Logic == null)
                smoke.Errors.Add("纯实体子物体或其 Logic 生成失败");

            // 共存：关联记录驱动位置并命中一次，命中转给 Logic，记录到期后实体随之销毁
            lane = SmokeOrigin + new Vector3(0f, 30f, 0f);
            CreateSmokeEntity(smoke, lane + new Vector3(3f, 0f, 0f), EFaction.Enemy, 0.5f);
            var linkedArgs = SmokeArgs(root, lane, 5f, 2f);
            linkedArgs.Policy.MaxHitCount = 1;
            var linked = subobjects.SpawnEntity(in linkedArgs, new SubobjectEntityOptions
            {
                LogicConfigId = LogicConfig_Subobject.DemoSmokeLogicId,
                WithRoutine = true,
            });
            smoke.Linked = SubobjectEntityRef.Of(linked);
            smoke.LinkedStart = lane;
            if (linked == null || !linked.comSubobject.RoutineHandle.IsValid)
            {
                smoke.Errors.Add("共存子物体或其关联记录生成失败");
                return;
            }

            // 来源链：以子物体为父级再抄写来源时，归属根仍是最初的单位
            var chained = SubobjectSource.Capture(linked, 0, 0, 0, out var chainedOrigin);
            if (chained.RootCreationIndex != root.creationIndex || chained.ParentCreationIndex != linked.creationIndex || chainedOrigin.Root.Entity != root)
                smoke.Errors.Add($"来源链错误 root={chained.RootCreationIndex}/{root.creationIndex}, parent={chained.ParentCreationIndex}/{linked.creationIndex}");
        }

        // 采样实体子物体的状态；到达校验时间后汇总结果并清理
        partial void SubobjectSmokeTick()
        {
            var smoke = _subobjectSmoke;
            if (smoke == null)
                return;

            var subobjects = _metaWorld.comUniSubobjects;
            if (subobjects == null)
            {
                _subobjectSmoke = null;
                return;
            }

            if (smoke.EntityOnly.TryGet(out var entityOnly) && entityOnly.hasComSubobject && entityOnly.comSubobject.Logic != null)
                smoke.EntityOnlyMaxTick = Mathf.Max(smoke.EntityOnlyMaxTick, entityOnly.comSubobject.Logic.GetVar<int>(LogicConfig_Subobject.DemoSmokeTickVar));

            if (smoke.Linked.TryGet(out var linked))
            {
                smoke.LinkedMaxTravel = Mathf.Max(smoke.LinkedMaxTravel, (linked.position - smoke.LinkedStart).magnitude);
                if (linked.hasComSubobject && linked.comSubobject.Logic != null)
                    smoke.LinkedMaxLogicHits = Mathf.Max(smoke.LinkedMaxLogicHits, linked.comSubobject.Logic.GetVar<int>(LogicConfig_Subobject.DemoSmokeHitVar));
            }

            if (subobjects.WorldTime < smoke.CheckTime)
                return;
            FinishSubobjectSmoke(subobjects, smoke);
        }

        private void FinishSubobjectSmoke(UniSubobjectsComponent subobjects, SubobjectSmoke smoke)
        {
            _subobjectSmoke = null;
            subobjects.HitSink = smoke.PreviousSink;
            var errors = smoke.Errors;

            var allyHits = smoke.HitsOn(smoke.LinearAlly);
            var enemyHits = smoke.HitsOn(smoke.LinearEnemy);
            if (allyHits != 0)
                errors.Add($"直线弹命中友方 {allyHits} 次");
            if (enemyHits != 1)
                errors.Add($"直线弹命中敌方 {enemyHits} 次，应为 1");
            if (subobjects.IsAlive(smoke.LinearBullet))
                errors.Add("直线弹命中次数用尽后仍存活");

            var pierceHits = 0;
            for (var i = 0; i < smoke.PierceTargets.Count; i++)
            {
                var hits = smoke.HitsOn(smoke.PierceTargets[i]);
                pierceHits += hits;
                if (hits != 1)
                    errors.Add($"穿透弹对第 {i} 个目标命中 {hits} 次，应为 1");
            }

            if (smoke.EntityOnlyMaxTick <= 0)
                errors.Add("纯实体子物体的 Logic 未被驱动");
            if (smoke.EntityOnly.TryGet(out _))
                errors.Add("纯实体子物体寿命结束后仍存活");

            if (smoke.LinkedMaxLogicHits < 1)
                errors.Add("共存子物体的 Logic 未收到命中");
            if (smoke.LinkedSinkHits != 1)
                errors.Add($"共存子物体经 HitSink 命中 {smoke.LinkedSinkHits} 次，应为 1");
            if (smoke.LinkedMaxTravel < 1f)
                errors.Add($"共存子物体未随记录移动，位移 {smoke.LinkedMaxTravel:F2}");
            if (smoke.Linked.TryGet(out _))
                errors.Add("共存子物体在记录到期后仍存活");

            for (var i = 0; i < smoke.Owned.Count; i++)
            {
                if (smoke.Owned[i].TryGet(out var entity) && !entity.hasComDeath)
                    entity.AddComDeath(null);
            }

            if (errors.Count == 0)
                G.Log($"SubobjectSmoke ok, enemyHits={enemyHits}, pierceHits={pierceHits}, entityOnlyTicks={smoke.EntityOnlyMaxTick}, linkedHits={smoke.LinkedMaxLogicHits}, linkedTravel={smoke.LinkedMaxTravel:F2}");
            else
                G.LogError($"SubobjectSmoke fail: {string.Join("; ", errors)}");
        }

        // radius 非正时不挂受击体
        private LogicEntity CreateSmokeEntity(SubobjectSmoke smoke, Vector3 position, EFaction faction, float radius)
        {
            var entity = _logicWorld.CreateEntity();
            entity.SetComTransform(position, Quaternion.identity, Vector3.one);
            entity.Faction = faction;
            if (radius > 0f)
                entity.SetComHitBody(radius, SmokeLayer);
            smoke.Owned.Add(SubobjectEntityRef.Of(entity));
            return entity;
        }

        // 沿 +x 飞行、只命中敌对冒烟目标的生成参数；speed 为 0 时不位移
        private static SubobjectSpawnArgs SmokeArgs(LogicEntity root, Vector3 position, float speed, float life)
        {
            var args = new SubobjectSpawnArgs();
            args.Source = SubobjectSource.Capture(root, 0, 0, 0, out args.Origin);
            args.Motion = new SubobjectMotion
            {
                Kind = speed > 0f ? ESubobjectMotionKind.Linear : ESubobjectMotionKind.None,
                Position = position,
                Direction = Vector3.right,
                Speed = speed,
                LifeLeft = life,
            };
            args.Policy = new SubobjectHitPolicy
            {
                Radius = 0.1f,
                TargetLayers = SmokeLayer,
                Factions = ESubobjectFactionFilter.Hostile,
            };
            return args;
        }

        /// <summary>
        /// 冒烟期间替换 UniSubobjectsComponent.HitSink：转发给原出口，并按目标与关联实体计数。
        /// </summary>
        private sealed class SubobjectSmoke : ISubobjectHitSink
        {
            public ISubobjectHitSink PreviousSink; // 冒烟结束时还原
            public float CheckTime;                // 到达此 WorldTime 后校验
            public readonly List<SubobjectEntityRef> Owned = new(); // 用例创建的实体，结束时统一销毁
            public readonly List<string> Errors = new();
            private readonly Dictionary<int, int> _hitsByTarget = new(); // 目标 creationIndex 到命中次数

            public SubobjectEntityRef LinearAlly;
            public SubobjectEntityRef LinearEnemy;
            public SubobjectHandle LinearBullet;
            public readonly List<SubobjectEntityRef> PierceTargets = new();
            public SubobjectEntityRef EntityOnly;
            public int EntityOnlyMaxTick;     // 采样到的 DemoSmokeTickVar 最大值
            public SubobjectEntityRef Linked;
            public Vector3 LinkedStart;
            public float LinkedMaxTravel;     // 采样到的离起点最大距离
            public int LinkedMaxLogicHits;    // 采样到的 DemoSmokeHitVar 最大值
            public int LinkedSinkHits;        // HitSink 收到的、Linked 为该实体的事件数

            /// <summary>
            /// 记录本帧命中后转发给原出口。
            /// </summary>
            public void OnSubobjectHits(UniSubobjectsComponent subobjects, List<SubobjectHitEvent> hits)
            {
                for (var i = 0; i < hits.Count; i++)
                {
                    var hit = hits[i];
                    _hitsByTarget.TryGetValue(hit.Target.CreationIndex, out var count);
                    _hitsByTarget[hit.Target.CreationIndex] = count + 1;
                    if (hit.Linked != null && Linked.TryGet(out var linked) && hit.Linked == linked)
                        LinkedSinkHits++;
                }
                PreviousSink?.OnSubobjectHits(subobjects, hits);
            }

            /// <summary>
            /// 目标累计被命中的次数。
            /// </summary>
            public int HitsOn(SubobjectEntityRef target)
            {
                _hitsByTarget.TryGetValue(target.CreationIndex, out var count);
                return count;
            }
        }
    }
}
