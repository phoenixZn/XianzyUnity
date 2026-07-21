using System.Xml;

namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool _RandomChanceCndCfg = Register(typeof(RandomChanceCndCfg), NodeCategory.Cnd);
    }

    //////////////////////////////////////////////////////////////////////////
    //静态配置
    public class RandomChanceCndCfg : ConditionBaseCfg
    {
        public float ProbPercent { get; protected set; } = 0f; //百分比概率

        public override System.Type NodeType()
        {
            return typeof(RandomChanceCnd);
        }

        public override bool ParseFromXml(XmlNode cndNode)
        {
            string str = XmlHelper.GetAttribute(cndNode, "ProbPercent");
            CLogger.LogAssert(!string.IsNullOrEmpty(str));
            ProbPercent = float.Parse(str);
            return base.ParseFromXml(cndNode);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 随机概率条件
    /// </summary>
    public class RandomChanceCnd : ConditionNodeBase
    {
        private RandomChanceCndCfg mCfg;
        private float mRandNum;

        //////////////////////////////////////////////////////////////////////////
        /// ICustomNode:
        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            mCfg = cfg as RandomChanceCndCfg;
#if CONSOLE_CLIENT
            mRandNum = G.Random.RandFloat(0f, 100f);
#else
            mRandNum = UnityEngine.Random.Range(0f, 100f);
#endif
            
        }

        public override void Destroy()
        {
            mRandNum = 0f;
            mCfg = null;
            base.Destroy();
        }

        //////////////////////////////////////////////////////////////////////////
        /// ConditionNodeBase:
        protected override bool Inner_ConditionCheck()
        {
            return mRandNum < mCfg.ProbPercent;
        }
    }
}