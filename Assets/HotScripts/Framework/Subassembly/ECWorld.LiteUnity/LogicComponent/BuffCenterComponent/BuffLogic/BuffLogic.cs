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
        public TargetEntityCheckerFunc TargetChecker { get; protected set; } // buff预加载检查器，用于在buff添加时对目标实体做额外的检查（比如免疫），返回true表示允许添加，false表示拒绝添加
        public int BuffTag { get; protected set; } // buff标签，mask格式，用于对buff进行批量管理
        public IBuffMergeRule MergeRule { get; protected set; } // 同类buff合并规则

        public FloatCfg TotalDuration = new FloatCfg(-1);
        
        public override System.Type NodeType()
        {
            return typeof(BuffLogic);
        }

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
        
        public BuffCfg WithDuration(string varID, float defaultValue = -1)
        {
            TotalDuration.SetVarID(varID);
            TotalDuration.ResetDefaultValue(defaultValue);
            return this;
        }
        
        public BuffCfg WithDuration(float defaultValue)
        {
            TotalDuration.ResetDefaultValue(defaultValue);
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
        private BuffCfg _buffCfg;

        private float _timeLeft; // 剩余持续时间，秒
        private float _timeTotal; // 总持续时间，秒
        public bool IsForever => _timeLeft <= 0; // 持续时间为配置值非正时视为永久
        private bool _isForceEnd; // 外部强制结束标记

        public int Level { get; set; }
        public int MaxLevel { get; set; }

        public float TimeLeft
        {
            get => _timeLeft;
        }

        public float TimeTotal
        {
            get => _timeTotal;
        }

        public BuffCfg BuffCfg
        {
            get { return _buffCfg; }
        }

        public BuffGenInfo BuffGenInfo
        {
            get { return GetGenInfo<BuffGenInfo>(); }
        }

        //////////////////////////////////////////////////////////////////////////
        /// IHasOwnerEntity / IHasSourceEntity:

        public LogicEntity OwnerEntity
        {
            get { return GetGenInfo<BuffGenInfo>()?.OwnerEntity; }
        }

        public LogicEntity SourceEntity
        {
            get { return GetGenInfo<BuffGenInfo>()?.SourceEntity; }
        }

        //////////////////////////////////////////////////////////////////////////
        /// CustomLogic：override

        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            if (!(context.GenInfo is BuffGenInfo))
            {
                G.LogError($"BuffLogic InitializeNode GenInfo is not BuffGenInfo, GenInfo={context.GenInfo.GetType()}");
            }

            _buffCfg = (BuffCfg)cfg;
            base.InitializeNode(cfg, context);
            _isForceEnd = false;
            _timeTotal = CalcDuration();
            _timeLeft = _timeTotal;
        }

        private float CalcDuration()
        {
            var durationCfg = _buffCfg.TotalDuration;
            var duration = durationCfg.GetValue(this);
            if (duration <= 0)
            {
                return duration;
            }

            // buff生成时的持续时间加成
            var genInfo = GetGenInfo<BuffGenInfo>();
            if (genInfo != null && genInfo.DurationAddRate != 0)
            {
                duration *= (1 + genInfo.DurationAddRate);
            }
            return duration;
        }

        public override void Destroy()
        {
            _buffCfg = null;
            _isForceEnd = false;

            base.Destroy();
        }

        public override bool CanStop()
        {
            if (IsForever)
                return false;

            if (_timeLeft <= 0)
            {
                return true;
            }

            return false;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IBuff:

        public bool IsFinished()
        {
            return _isForceEnd || CanStop();
        }

        public void UpdateBuff(float dt)
        {
            if (!IsForever)
            {
                _timeLeft -= dt;
                if (_timeLeft <= 0)
                    return;
            }
            Update(dt);
        }

        //////////////////////////////////////////////////////////////////////////
        /// IForceEnd:

        public void ForceEnd()
        {
            _isForceEnd = true;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 时间与层数

        public void SetTime(float duration)
        {
            _timeTotal = duration;
            _timeLeft = duration;
        }

        public void ResetTime()
        {
            _timeTotal = CalcDuration();
            _timeLeft = _timeTotal;
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
            var genInfo = GetGenInfo<BuffGenInfo>();
            if (genInfo != null)
            {
                Level = genInfo.BuffLevel;
                MaxLevel = genInfo.BuffMaxLevel;
            }
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
