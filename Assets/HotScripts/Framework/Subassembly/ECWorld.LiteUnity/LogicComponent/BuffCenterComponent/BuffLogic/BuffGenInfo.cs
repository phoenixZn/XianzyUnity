using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Buff 生成信息：创建 <see cref="BuffLogic"/> 的运行时初始数据，走对象池，用毕由工厂回收。
    /// </summary>
    public class BuffGenInfo : CustomLogicGenInfo, IHasLogicWorld, IHasMetaWorld, IHasOwnerEntity, IHasSourceEntity
    {
        public int BuffLevel { get; protected set; } // 初始层数，上身后由 BuffLogic.Level 作为运行时真源
        public int BuffMaxLevel { get; protected set; } // 最大层数，上身后由 BuffLogic.MaxLevel 作为运行时真源
        public float DurationAddRate { get; internal set; } // 持续时间加成比例；免疫在 AddNewBuff 中扣减

        //////////////////////////////////////////////////////////////////////////
        /// IHasLogicWorld / IHasMetaWorld / IHasOwnerEntity / IHasSourceEntity:

        public LogicWorld LogicWorld { get; protected set; }
        public MetaWorld MetaWorld { get; protected set; }
        public LogicEntity OwnerEntity { get; internal set; } // 宿主实体；AddNewBuff 以实际挂载实体为准
        public LogicEntity SourceEntity { get; protected set; } // 来源实体，环境/系统 Buff 可为 null

        //////////////////////////////////////////////////////////////////////////
        /// CustomLogicGenInfo：override

        public override void Destroy()
        {
            LogicWorld = null;
            MetaWorld = null;
            OwnerEntity = null;
            SourceEntity = null;
            BuffLevel = 0;
            BuffMaxLevel = 0;
            DurationAddRate = 0;
            base.Destroy();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        // 写入宿主、来源、层数，并从 owner 推导 LogicWorld。sourceEntity 允许为 null。
        internal void Init(MetaWorld metaWorld, LogicEntity ownerEntity, LogicEntity sourceEntity, int buffLevel, int buffMaxLevel)
        {
            if (metaWorld == null)
                G.LogError("BuffGenInfo Init 异常, MetaWorld 为空");
            if (ownerEntity == null)
                G.LogError("BuffGenInfo Init 异常, OwnerEntity 为空");

            var logicWorld = ownerEntity != null ? ownerEntity.OwnerWorld : null;
            if (logicWorld == null)
                G.LogError("BuffGenInfo Init 异常, LogicWorld 为空");

            LogicWorld = logicWorld;
            MetaWorld = metaWorld;
            OwnerEntity = ownerEntity;
            SourceEntity = sourceEntity;
            BuffLevel = buffLevel;
            BuffMaxLevel = buffMaxLevel;
            DurationAddRate = 0;
        }

        // 从对象池取出并初始化 Buff 所需的 GenInfo。LogicConfigID / ConfigContainerName 由调用方随后填写。
        internal static BuffGenInfo New(ICustomLogicService svc, MetaWorld metaWorld, LogicEntity ownerEntity, LogicEntity sourceEntity, int buffLevel = 1, int buffMaxLevel = 1)
        {
            var genInfo = svc.NewGenInfo<BuffGenInfo>();
            genInfo.Init(metaWorld, ownerEntity, sourceEntity, buffLevel, buffMaxLevel);
            return genInfo;
        }
    }
}
