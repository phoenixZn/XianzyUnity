using System.Collections.Generic;
using System.Xml;

namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        private static bool _StateTransitionNodeCfg = Register(typeof(StateTransitionNodeCfg), NodeCategory.StateTransition);
    }

    /// <summary>
    /// 静态配置: 状态转换节点
    /// </summary>
    public class StateTransitionNodeCfg : ICustomNodeCfg, IParseFromXml
    {
        public ICustomNodeCfg ConditionCfg { get; protected set; } //判断条件配置
        public string TrueStateID { get; protected set; } //条件达成 将跳转的stateID
        public string FalseStateID { get; protected set; } //条件不达成 将跳转的stateID
        public float CheckInterval { get; protected set; } = 0; //检查间隔

        public System.Type NodeType()
        {
            return typeof(StateTransitionNode);
        }

        public StateTransitionNodeCfg()
        {
        }

        public StateTransitionNodeCfg(ICustomNodeCfg cndCfg, string trueID = null, string falseID = null)
        {
            ConditionCfg = cndCfg;
            TrueStateID = trueID;
            FalseStateID = falseID;
        }

        public bool ParseFromXml(XmlNode xmlNode)
        {
            ConditionCfg = CLHelper.CreateNodeCfg(xmlNode.SelectSingleNode("Condition"));
            TrueStateID = XmlHelper.GetAttribute(xmlNode, "TrueStateID");
            FalseStateID = XmlHelper.GetAttribute(xmlNode, "FalseStateID");

            if (!CLogger.LogAssert(!string.IsNullOrEmpty(TrueStateID) || !string.IsNullOrEmpty(FalseStateID)))
            {
                return false;
            }

            var categoryCnd = NodeConfigTypeRegistry.GetNodeCfgCategory(ConditionCfg.GetType());
            if (!CLogger.LogAssert(categoryCnd == NodeCategory.Cnd))
            {
                return false;
            }

            CheckInterval = XmlHelper.GetFloat(xmlNode, "CheckOnTick");

            return true;
        }
    }


    /// <summary>
    /// 状态转换节点类
    /// </summary>
    public class StateTransitionNode : CustomNode, INeedUpdate
    {
        //条件判断后会转向什么状态，不填就是不转换状态
        public string mTrueStateID;
        public string mFalseStateID;
        public float mCfgCheckInterval;
        public float mCheckCDRemain;
        public ConditionNodeBase mConditionNode;


        public StateTransitionNode()
        {
            mTrueStateID = null;
            mFalseStateID = null;
            mCfgCheckInterval = 0f;
            mCheckCDRemain = 0f;
            mConditionNode = null;
        }


        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);

            var theCfg = cfg as StateTransitionNodeCfg;
            mConditionNode = mContext.Factory.CreateCustomNode(theCfg.ConditionCfg, context) as ConditionNodeBase;

            mTrueStateID = theCfg.TrueStateID;
            mFalseStateID = theCfg.FalseStateID;

            // 扩展配置：条件检查间隔，对于某些计算量比较大的条件检查，不能每帧都做
            mCfgCheckInterval = theCfg.CheckInterval;
            mCheckCDRemain = 0f;
        }

        public override void Destroy()
        {
            mContext.Factory.DestroyCustomNode(mConditionNode);
            mConditionNode = null;

            mTrueStateID = null;
            mFalseStateID = null;
            mCfgCheckInterval = 0f;
            mCheckCDRemain = 0f;
            base.Destroy();
        }

        public override void Reset()
        {
            base.Reset();
            mConditionNode?.Reset();
        }

        public override void Activate()
        {
            base.Activate();
            mConditionNode?.Activate();
        }

        public override void Deactivate()
        {
            base.Deactivate();
            mConditionNode?.Deactivate();
        }

        public override void CollectInterfaceInChildren<T>(ref List<T> interfaceList)
        {
            if (mConditionNode != null)
            {
                TraverseCollectInterface<T>(ref interfaceList, mConditionNode);
            }
        }


        public float Update(float dt)
        {
            if (mConditionNode is INeedUpdate tickCnd)
            {
                tickCnd.Update(dt);
            }

            if (mCheckCDRemain > 0)
            {
                mCheckCDRemain -= dt;
            }

            return dt;
        }

        public string CheckTransitions()
        {
            if (mConditionNode == null)
            {
                return null;
            }

            if (mCfgCheckInterval <= 0f)
            {
                return Inner_CheckTransition();
            }

            if (mCheckCDRemain <= 0)
            {
                mCheckCDRemain = mCfgCheckInterval;
                return Inner_CheckTransition();
            }

            return null;
        }


        private string Inner_CheckTransition()
        {
            string next_state = null;
            if (mConditionNode.IsConditionReached())
            {
                next_state = mTrueStateID;
            }
            else
            {
                next_state = mFalseStateID;
            }

            return next_state;
        }
    }
}