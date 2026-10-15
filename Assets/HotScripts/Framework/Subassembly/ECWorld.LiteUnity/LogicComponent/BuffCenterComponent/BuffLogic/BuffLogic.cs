using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary> 对即将承受 Buff 的目标做额外校验；返回 true 表示允许施加，false 表示拒绝（如免疫）。 </summary>
    public delegate bool TargetEntityCheckerFunc(LogicEntity targetEntity);

    /// <summary>
    /// Buff 配置的玩法扩展点：上身前预检测、批量管理标签、同类合并规则。
    /// </summary>
    public interface IEntityBuffCfg
    {
        /// <summary> 非 null 时在 <see cref="BuffCenterComponent.AddNewBuff"/> 中对宿主实体调用；返回 false 则不上 Buff。 </summary>
        int BuffTag { get; }
        TargetEntityCheckerFunc TargetChecker { get; }
        IBuffMergeRule MergeRule { get; }
    }

    /// <summary>
    /// Buff 静态配置：在 <see cref="CustomLogicCfg"/> 之上扩展 Buff 专属玩法字段。
    /// </summary>
    public class BuffCfg : CustomLogicCfg, IEntityBuffCfg
    {
        public override System.Type NodeType()
        {
            return typeof(BuffLogic);
        }

        public TargetEntityCheckerFunc TargetChecker { get; protected set; } // buff预加载检查器，用于在buff添加时对目标实体做额外的检查（比如免疫），返回true表示允许添加，false表示拒绝添加
        public int BuffTag { get; protected set; } // buff标签，mask格式，用于对buff进行批量管理
        public IBuffMergeRule MergeRule { get; protected set; } // 同类buff合并规则

        public BuffCfg WithTargetChecker(TargetEntityCheckerFunc checkFuc)
        {
            TargetChecker = checkFuc;
            return this;
        }

        public BuffCfg WithBuffTag(int buffTag)
        {
            BuffTag = buffTag;
            return this;
        }

        public BuffCfg WithMergeRule(IBuffMergeRule mergeRule)
        {
            MergeRule = mergeRule;
            return this;
        }

        public BuffCfg(int id, List<ICustomNodeCfg> nodeCfgList, System.Type logicType, string desc = null) : base(id, nodeCfgList, logicType, desc)
        {
        }
    }

    /// <summary>
    /// Buff 运行时逻辑：由 <see cref="BuffCenterComponent"/> 持有并驱动，负责持续时间倒计时、
    /// 层数管理，并把上身/移除/状态变化通知传播给各 CustomNode。
    /// </summary>
    public partial class BuffLogic : EntityCmdLogic, IBuff, IHasOwnerEntity, IHasSourceEntity, IForceEnd
    {
        private BuffCfg m_buffCfg;
        private BuffGenInfo m_buffGenInfo;

        private float m_timeLeft; // 剩余持续时间，秒
        private float m_timeTotal; // 总持续时间，秒
        private bool m_isForever; // 持续时间为配置值非正时视为永久
        private bool m_isForceEnd; // 外部强制结束标记

        public int Level { get; set; }
        public int MaxLevel { get; set; }

        public float TimeLeft
        {
            get => m_timeLeft;
        }

        public float TimeTotal
        {
            get => m_timeTotal;
        }

        public BuffCfg BuffCfg
        {
            get { return m_buffCfg; }
        }

        public BuffGenInfo BuffGenInfo
        {
            get { return m_buffGenInfo; }
        }

        //////////////////////////////////////////////////////////////////////////
        /// IHasOwnerEntity / IHasSourceEntity:

        public LogicEntity OwnerEntity
        {
            get { return m_buffGenInfo.Owner; }
        }

        public LogicEntity SourceEntity
        {
            get { return m_buffGenInfo.Sourcer; }
        }

        //////////////////////////////////////////////////////////////////////////
        /// CustomLogic：override

        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            PreInitializeNode(cfg, context);
            base.InitializeNode(cfg, context);
            m_isForceEnd = false;
            m_timeTotal = CalcDuration();
            m_timeLeft = m_timeTotal;
            m_isForever = m_timeLeft <= 0;
        }

        private void PreInitializeNode(ICustomNodeCfg cfg, CustomNodeContext context)
        {
            m_buffCfg = (BuffCfg)cfg;
            m_buffGenInfo = (BuffGenInfo)context.GenInfo;
        }

        private float CalcDuration()
        {
            var durationCfg = GetVar<FloatCfg>(CvKey.CV_BuffDuration);
            var duration = durationCfg.GetValue(this);
            if (duration <= 0)
            {
                return duration;
            }

            if (m_buffGenInfo.DurationAddSeconds != 0)
            {
                duration += m_buffGenInfo.DurationAddSeconds;
            }
            if (m_buffGenInfo.SourceDurationAddRate != 0)
            {
                duration *= (1 + m_buffGenInfo.SourceDurationAddRate);
            }
            // buff生成时的持续时间加成
            if (m_buffGenInfo.DurationAddRate != 0)
            {
                duration *= (1 + m_buffGenInfo.DurationAddRate);
            }
            return duration;
        }

        public override void Destroy()
        {
            m_buffCfg = null;
            m_buffGenInfo.Clear();
            m_isForceEnd = false;

            base.Destroy();
        }

        public override bool CanStop()
        {
            if (m_isForever)
                return false;

            if (m_timeLeft <= 0)
            {
                return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IBuff:

        public bool IsFinished()
        {
            return m_isForceEnd || CanStop();
        }

        public void UpdateBuff(float dt)
        {
            if (!m_isForever)
            {
                m_timeLeft -= dt;
                if (m_timeLeft <= 0)
                    return;
            }

            Update(dt);
        }

        public void Upgrade()
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// IForceEnd:

        public void ForceEnd()
        {
            m_isForceEnd = true;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 时间与层数

        public void SetTime(float duration)
        {
            m_timeTotal = duration;
            m_timeLeft = duration;
        }

        public void ResetTime()
        {
            m_timeTotal = CalcDuration();
            m_timeLeft = m_timeTotal;
        }

        public bool IsMaxLevel()
        {
            return Level >= MaxLevel;
        }

        public bool HasBuffTag(int buffTag)
        {
            return (BuffCfg.BuffTag & buffTag) == buffTag;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 外界通知传播
        // 系统外界信息的通知, 将通知传播给各个CustomNode和其子Node

        private List<IEntityAddBuffNotify> mBuffAddList = new List<IEntityAddBuffNotify>();

        private List<IBuffPreviousRemove> mBuffPreviousRemoveList = new List<IBuffPreviousRemove>();

        private List<IBuffStateChanged> mBuffStateChangedList = new List<IBuffStateChanged>();

        protected override void ClearInterfaceCache()
        {
            mBuffAddList.Clear();
            mBuffPreviousRemoveList.Clear();
            mBuffStateChangedList.Clear();
            base.ClearInterfaceCache();
        }

        protected override void CacheInterface(CustomNode node)
        {
            base.CacheInterface(node);
            TraverseCollectInterface(ref mBuffAddList, node);
            TraverseCollectInterface(ref mBuffPreviousRemoveList, node);
            TraverseCollectInterface(ref mBuffStateChangedList, node);
        }

        public virtual void OnEntityAddBuff(LogicEntity e)
        {
            Level = m_buffGenInfo.BuffLevel;
            MaxLevel = m_buffGenInfo.BuffMaxLevel;
            foreach (var c in mBuffAddList)
            {
                var node = c as ICustomNode;
                if (node != null && node.IsActive)
                {
                    c.OnEntityAddBuff(e);
                }
            }
        }

        public virtual void OnBuffStateChanged(LogicEntity e)
        {
            foreach (var c in mBuffStateChangedList)
            {
                var node = c as ICustomNode;
                if (node != null && node.IsActive)
                {
                    c.OnBuffStateChanged(e, this);
                }
            }
        }

        public virtual void OnBuffPreviousRemove()
        {
            Level = 0;
            foreach (var c in mBuffPreviousRemoveList)
            {
                var node = c as ICustomNode;
                if (node != null && node.IsActive)
                {
                    c.OnBuffPreviousRemove();
                }
            }
        }
    }
}
