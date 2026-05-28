using System.Collections.Generic;

namespace HotUpdate.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool EveryXSecondsBhvCfg = Register(typeof(EveryXSecondsBhvCfg), NodeCategory.Bhv);
    }

    /// <summary>
    /// 静态配置：每隔 intervalSeconds 秒执行一次 actionNodes（子序列可包含 UpdateCall 等，可持续多帧）。
    /// </summary>
    public class EveryXSecondsBhvCfg : ICustomNodeCfg
    {
        public float IntervalSeconds { get; protected set; }
        public List<ICustomNodeCfg> ActionNodes { get; protected set; }

        public System.Type NodeType()
        {
            return typeof(EveryXSecondsBhv);
        }

        public EveryXSecondsBhvCfg(float intervalSeconds, List<ICustomNodeCfg> actionNodes)
        {
            IntervalSeconds = intervalSeconds;
            ActionNodes = actionNodes ?? new List<ICustomNodeCfg>();
        }
    }

    /// <summary>
    /// 运行时：用 Update 做计时，每到 interval 秒就创建并运行一次 action 子序列，子序列跑完后再等 interval 秒，循环直至父逻辑结束。
    /// </summary>
    public class EveryXSecondsBhv : BehaviorNodeBase, INeedStopCheck
    {
        private EveryXSecondsBhvCfg mCfg;
        private float mAcc;
        private SequenceBhv mActionSeq;
        private SequenceBhvCfg mActionSeqCfg;

        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            mCfg = cfg as EveryXSecondsBhvCfg;
            if (mCfg == null || mCfg.ActionNodes == null || mCfg.ActionNodes.Count == 0)
                return;
            mActionSeqCfg = new SequenceBhvCfg(mCfg.ActionNodes, 1, 0f);
        }

        protected override void OnBegin()
        {
            // 刚进入时先触发一次，再进入 CD（首次 Update 即满足 mAcc >= IntervalSeconds）
            mAcc = mCfg != null ? mCfg.IntervalSeconds : 0f;
            mActionSeq = null;
        }

        protected override float OnUpdate(float dt)
        {
            if (mCfg == null || mActionSeqCfg == null)
                return dt;

            if (mActionSeq != null)
            {
                float dtRemain = mActionSeq.Update(dt);
                if (!IsActive)
                    return 0f;
                if (mActionSeq is INeedStopCheck needStop && needStop.CanStop())
                {
                    mContext.Factory.DestroyCustomNode(mActionSeq);
                    mActionSeq = null;
                    return dtRemain;
                }
                return 0f;
            }

            mAcc += dt;
            if (mAcc < mCfg.IntervalSeconds)
                return 0f;

            mAcc -= mCfg.IntervalSeconds;
            var seq = mContext.Factory.CreateCustomNode(mActionSeqCfg, mContext) as SequenceBhv;
            if (seq == null)
                return dt;
            mActionSeq = seq;
            mActionSeq.Activate();
            float remain = mActionSeq.Update(dt);
            if (!IsActive)
                return 0f;
            if (mActionSeq is INeedStopCheck ns && ns.CanStop())
            {
                mContext.Factory.DestroyCustomNode(mActionSeq);
                mActionSeq = null;
                return remain;
            }
            return 0f;
        }

        public override void Destroy()
        {
            if (mActionSeq != null)
            {
                mContext.Factory.DestroyCustomNode(mActionSeq);
                mActionSeq = null;
            }
            mActionSeqCfg = null;
            mCfg = null;
            base.Destroy();
        }

        public bool CanStop()
        {
            return false;
        }
    }
}
