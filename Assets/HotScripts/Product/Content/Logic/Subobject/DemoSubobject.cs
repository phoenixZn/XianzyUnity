using System.Collections.Generic;
using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 子物体示例：实体保存血量与受击体，直线弹由关联记录推进并复用实体 View，命中经世界出口扣血。
    /// </summary>
    internal static partial class DemoSubobject
    {
        // Idle 状态两次发射之间的间隔，秒。
        internal const float FireInterval = 1f / 10f;
        // YooAsset 按文件名收集的球体资源地址，Loading 预热与发射时共用。
        internal const string BulletViewAsset = "DemoSubobjectBullet2D";
        // 弹体的飞行速度，世界单位/秒。
        private const float BulletSpeed = 8f;
        // 记录寿命，秒；到期后关联实体和 View 一同回收。
        private const float BulletLife = 2.5f;
        // 弹体的逻辑查询半径，与球体 Prefab 的视觉范围配合。
        private const float BulletRadius = 0.15f;
        // 命中一次扣除的整数血量。
        private const long BulletDamage = 1L;
        // 弹体可结算的命中次数，用尽后连同关联实体回收。
        private const int BulletMaxHitCount = 1;
        // 世界命中出口据此识别本示例的结算事件。
        private const int BulletEffectId = 1;

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 世界初始化时安装；冒烟测试可临时包装该出口并在结束后还原。
        internal static void InstallHitSink(UniSubobjectsComponent subobjects)
        {
            subobjects.HitSink = new DemoHitSink();
        }

        
        // Demo 单位出生时的当前血量与上限。
        private const long InitialHp = 20L;
        // Demo 单位的球形受击范围，世界单位。
        private const float TargetRadius = 0.5f;
        // 独立于冒烟测试受击层，只让示例子弹查询 Demo 单位。
        private const uint TargetLayer = 1u << 0;
        // 在创建主状态机前提供血量和受击能力，Born 设置位置后即可进入目标快照。
        internal static void InitializeEntity(LogicEntity entity)
        {
            entity.SetComHp(InitialHp, InitialHp);
            entity.SetComHitBody(TargetRadius, TargetLayer);
        }

        // 随机角度只决定初始方向，不读取受击目标；后续位姿完全交给关联记录。
        internal static void Fire(LogicEntity owner, UniSubobjectsComponent subobjects)
        {
            if (owner == null || subobjects == null)
                return;
            if (!owner.isEnabled || !owner.hasComTransform)
                return;

            var angle = G.Random.RandFloat(0f, Mathf.PI * 2f);
            var args = new SubobjectSpawnArgs
            {
                Motion = new SubobjectMotion
                {
                    Kind = ESubobjectMotionKind.Linear,
                    Position = owner.position,
                    Direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f),
                    Speed = BulletSpeed,
                    LifeLeft = BulletLife,
                },
                Policy = new SubobjectHitPolicy
                {
                    Radius = BulletRadius,
                    TargetLayers = TargetLayer,
                    Factions = ESubobjectFactionFilter.Hostile | ESubobjectFactionFilter.Ally | ESubobjectFactionFilter.Neutral,
                    IgnoreSource = true,
                    MaxHitCount = BulletMaxHitCount,
                    DestroyWhenExhausted = true,
                    EffectId = BulletEffectId,
                },
            };
            args.Source = SubobjectSource.Capture(owner, 0, 0, 0, out args.Origin);

            // 实体记录不消费 ViewKey，必须显式通过现有 View 加载入口绑定表现。
            var bullet = subobjects.SpawnEntity(in args, new SubobjectEntityOptions { WithRoutine = true });
            bullet?.RequestViewLoad<PooledAssetViewWrapper>(BulletViewAsset);
        }

        // 命中特效在世界坐标播放；命令行未编入 Unity 分部时调用被抹掉。
        static partial void PlayHitFx(Vector3 position);

        /// <summary>
        /// Demo 世界的命中结算出口，只处理本示例的弹体事件，并将致死命中交给现有主状态机。
        /// </summary>
        private sealed class DemoHitSink : ISubobjectHitSink
        {
            //////////////////////////////////////////////////////////////////////////
            /// ISubobjectHitSink:

            /// <summary>
            /// 结算本帧 hits 中仍有效的目标并播放命中特效；归零后立即进入死亡状态，避免重复扣血。
            /// </summary>
            public void OnSubobjectHits(UniSubobjectsComponent subobjects, List<SubobjectHitEvent> hits)
            {
                for (var i = 0; i < hits.Count; i++)
                {
                    var hit = hits[i];
                    if (hit.EffectId != BulletEffectId || !hit.Target.TryGet(out var target) || target.hasComDeath || !target.hasComHp)
                        continue;

                    var hp = target.comHp;
                    if (hp.Hp <= 0)
                        continue;

                    hp.SetHP(System.Math.Max(0L, hp.Hp - BulletDamage));
                    PlayHitFx(hit.Point);
                    if (hp.Hp > 0)
                        continue;

                    // Nt_ForceDeath 直接进入 MST_Die，保留 Move 对点击 Nt_Death 的既有延迟行为。
                    if (target.hasComFSM)
                        target.comFSM.HandleEntityCommand(target, new EntityCommand { CmdType = EntityCmdType.Nt_ForceDeath });
                    else
                        target.AddComDeath(null);
                }
            }
        }
    }
}
