using System;
using UnityEngine;

namespace Xease.CoreGame
{
    // 基准运动方式
    public enum ESubobjectMotionKind : byte
    {
        None = 0,   // 不位移，如地雷
        Linear = 1, // 沿 Direction 匀速
        Homing = 2, // 先朝 HomingTarget 转向再匀速前进；目标丢失后保持当前方向
    }

    // 允许命中的阵营关系，按位组合；关系由 SubobjectSource.Faction 与目标阵营比较得出
    [Flags]
    public enum ESubobjectFactionFilter : byte
    {
        None = 0,
        Hostile = 1 << 0, // 双方都有非中立阵营且不同
        Ally = 1 << 1,    // 双方都有非中立阵营且相同
        Neutral = 1 << 2, // 任一方为中立或未设置阵营
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 批处理记录的句柄。槽位每次释放都会递增 Generation，旧句柄因此失效；默认值即无效句柄。
    /// </summary>
    public struct SubobjectHandle : IEquatable<SubobjectHandle>
    {
        public short Routine;  // 所属套路在 UniSubobjectsComponent 中的序号
        public int Slot;       // 套路内句柄间接表的下标
        public int Generation; // 从 1 起计；0 表示无效

        public bool IsValid => Generation != 0;

        public bool Equals(SubobjectHandle other)
        {
            return Routine == other.Routine && Slot == other.Slot && Generation == other.Generation;
        }

        public override bool Equals(object obj)
        {
            return obj is SubobjectHandle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (Slot * 397) ^ (Generation * 31) ^ Routine;
        }

        public static bool operator ==(SubobjectHandle a, SubobjectHandle b) => a.Equals(b);
        public static bool operator !=(SubobjectHandle a, SubobjectHandle b) => !a.Equals(b);

        public override string ToString()
        {
            return $"Subobject({Routine}:{Slot}#{Generation})";
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 防复用的实体引用。Entitas 会复用实体对象，creationIndex 变化或实体已禁用即视为原实体已不在。
    /// </summary>
    public struct SubobjectEntityRef
    {
        public LogicEntity Entity;
        public int CreationIndex; // 记录时的 creationIndex，用来识别对象被复用

        /// <summary>
        /// 记下实体当前身份；entity 为 null 时返回空引用。
        /// </summary>
        public static SubobjectEntityRef Of(LogicEntity entity)
        {
            if (entity == null)
                return default;
            return new SubobjectEntityRef { Entity = entity, CreationIndex = entity.creationIndex };
        }

        /// <summary>
        /// 原实体仍在世界中时返回 true 并给出实体；已销毁或对象已被复用时返回 false。
        /// </summary>
        public bool TryGet(out LogicEntity entity)
        {
            entity = Entity;
            if (entity != null && entity.isEnabled && entity.creationIndex == CreationIndex)
                return true;
            entity = null;
            return false;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 来源快照：出生时按值抄写，父级销毁后仍可用于记账与过滤。全部字段为值类型，可整体拷进批处理缓冲。
    /// 由子物体再生成子物体时沿用父级的 Root，战果始终记到最初的单位。
    /// </summary>
    public struct SubobjectSource
    {
        public long RootEntityId;       // 战果归属单位的 LogicEntity.ID
        public int RootCreationIndex;   // 战果归属单位的 creationIndex；世界内不复用，可作记账键
        public long ParentEntityId;     // 直接生成者的 LogicEntity.ID，可能是另一个子物体
        public int ParentCreationIndex; // 直接生成者的 creationIndex
        public long PlayerUID;          // 局内玩家；0 表示无玩家归属
        public EFaction Faction;        // 出生时阵营，命中过滤只读这里
        public int RootUnitTid;         // 归属单位的配置 ID，由调用方传入
        public int SkillTid;            // 产生本子物体的技能配置 ID
        public int SubobjectTid;        // 子物体配置 ID，供 CategoryStatsVolumeSubobject 等分类使用

        /// <summary>
        /// 以 parent 为直接生成者抄写来源。parent 本身是子物体时沿用其 Root、玩家与阵营；否则 parent 即归属根。
        /// parent 为 null 表示无来源（如关卡机关），调用方可在返回后自行填写 Faction。
        /// origin 返回同一来源的主线程侧引用，与返回值成对交给生成接口。
        /// </summary>
        public static SubobjectSource Capture(LogicEntity parent, int skillTid, int subobjectTid, int parentUnitTid, out SubobjectOrigin origin)
        {
            var source = new SubobjectSource { SkillTid = skillTid, SubobjectTid = subobjectTid };
            origin = default;
            if (parent == null)
                return source;

            source.ParentEntityId = parent.ID;
            source.ParentCreationIndex = parent.creationIndex;
            origin.Parent = SubobjectEntityRef.Of(parent);

            if (parent.hasComSubobject)
            {
                var parentCom = parent.comSubobject;
                source.RootEntityId = parentCom.Source.RootEntityId;
                source.RootCreationIndex = parentCom.Source.RootCreationIndex;
                source.RootUnitTid = parentCom.Source.RootUnitTid;
                source.PlayerUID = parentCom.Source.PlayerUID;
                source.Faction = parent.hasComFaction ? parent.Faction : parentCom.Source.Faction;
                origin.Root = parentCom.Origin.Root;
                origin.Player = parentCom.Origin.Player ?? parent.GetPlayerInfo();
                return source;
            }

            source.RootEntityId = parent.ID;
            source.RootCreationIndex = parent.creationIndex;
            source.RootUnitTid = parentUnitTid;
            source.Faction = parent.Faction;
            origin.Root = origin.Parent;
            origin.Player = parent.GetPlayerInfo();
            source.PlayerUID = origin.Player != null ? origin.Player.PlayerUID : 0;
            return source;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 来源的主线程侧引用：归属根、直接父级与局内玩家。与 SubobjectSource 成对保存，不进批处理热数据。
    /// </summary>
    public struct SubobjectOrigin
    {
        public SubobjectEntityRef Root;   // 归属根；根实体销毁后 TryGet 返回 false
        public SubobjectEntityRef Parent; // 直接生成者
        public InGamePlayerInfo Player;   // 允许 null，如关卡机关
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 运动热数据，每帧由所属套路整批积分。
    /// </summary>
    public struct SubobjectMotion
    {
        public ESubobjectMotionKind Kind;
        public Vector3 Position;
        public Vector3 PrevPosition; // 本帧积分前的位置；与 Position 连成线段做扫掠命中
        public Vector3 Direction;    // 单位向量；追踪时每帧改写
        public float Speed;          // 世界单位/秒
        public float TurnRate;       // 追踪转向角速度，弧度/秒；非正表示每帧直接对准目标
        public int HomingTarget;     // 追踪目标的 creationIndex，目标需带 HitBodyComponent；0 表示无目标
        public float LifeLeft;       // 剩余寿命，秒；生成时传非正数表示不按时间结束
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 命中规则，生成后只读。
    /// </summary>
    public struct SubobjectHitPolicy
    {
        public float Radius;                     // 自身查询半径，与目标 HitBodyComponent.Radius 相加判定
        public uint TargetLayers;                // 与目标 HitBodyComponent.Layer 按位与；0 表示不参与命中
        public ESubobjectFactionFilter Factions; // 允许命中的阵营关系
        public int MaxHitCount;                  // 累计命中上限；0 表示不因次数结束
        public float HitInterval;                // 两次命中的全局间隔，秒；0 表示同一帧可连续命中多个目标
        public int PerTargetMax;                 // 同一目标命中上限；0 表示不限
        public float PerTargetInterval;          // 同一目标两次命中的间隔，秒；0 表示不限
        public bool IgnoreSource;                // 为 true 时跳过 Root 与 Parent
        public bool DestroyWhenExhausted;        // MaxHitCount 用尽时回收；为 false 时继续飞行但不再命中
        public int EffectId;                     // 不透明结算号，框架不解释，原样放进命中事件
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 命中状态，由所属套路维护。
    /// </summary>
    public struct SubobjectHitState
    {
        public int HitCount;      // 已结算的命中次数
        public float NextHitTime; // 下次允许命中的世界时间戳，秒
        public int MemoryHead;    // 同目标记忆链表头，指向 SubobjectHitMemoryPool；-1 表示无记忆
        public bool Expired;      // 已到期，等待本帧回收阶段处理
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 生成一条批处理记录（或一个实体子物体）所需的全部初始数据。
    /// </summary>
    public struct SubobjectSpawnArgs
    {
        public SubobjectSource Source;
        public SubobjectOrigin Origin;
        public SubobjectMotion Motion;
        public SubobjectHitPolicy Policy;
        public int ViewKey;   // 交给 ISubobjectViewSink 的表现键；0 表示无表现
        public short Routine; // 目标套路序号；0 为默认套路
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 实体子物体的生成选项。寿命取 SubobjectSpawnArgs.Motion.LifeLeft：带记录时由记录计时，否则转为 ComLife。
    /// </summary>
    public struct SubobjectEntityOptions
    {
        public int LogicConfigId;     // 自定义逻辑配置 ID；0 表示不创建 Logic
        public string LogicContainer; // 逻辑配置容器名；为空时用 UniSubobjectsComponent.DefaultLogicContainer
        public bool WithRoutine;      // 为 true 时同时生成关联的批处理记录（基准行为）
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 一次命中。由套路在步进阶段写入缓冲，随后在主线程派发给 ISubobjectHitSink 与关联实体的 Logic。
    /// </summary>
    public struct SubobjectHitEvent
    {
        public SubobjectHandle Handle;
        public SubobjectEntityRef Target;
        public long TargetEntityId;    // 目标的 LogicEntity.ID
        public Vector3 Point;          // 扫掠线段上离目标中心最近的点
        public int EffectId;
        public SubobjectSource Source;
        public SubobjectOrigin Origin;
        public LogicEntity Linked;     // 共存时的实体子物体；纯批处理记录为 null
    }
}
