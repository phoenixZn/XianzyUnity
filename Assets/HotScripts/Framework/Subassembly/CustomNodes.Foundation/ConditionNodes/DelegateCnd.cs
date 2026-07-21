using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;

namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool _DelegateFunctionCndCfg = Register(typeof(DelegateConditionCfg), NodeCategory.Cnd);
    }

    public delegate bool NodeParamCndFunction(CustomNode node);
    //////////////////////////////////////////////////////////////////////////
    //静态配置
    public class DelegateConditionCfg : ConditionBaseCfg
    {
        public NodeParamCndFunction CndFunc { get; protected set; }

        public override System.Type NodeType()
        {
            return typeof(DelegateCondition);
        }

        public DelegateConditionCfg()
        {
        }

        public DelegateConditionCfg(NodeParamCndFunction cndFunc)
        {
            CndFunc = cndFunc;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 运行时：调用简单函数
    /// </summary>
    public class DelegateCondition : ConditionNodeBase
    {
        private DelegateConditionCfg mCfg;

        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            mCfg = cfg as DelegateConditionCfg;
        }

        public override void Destroy()
        {
            mCfg = null;
            base.Destroy();
        }

        //////////////////////////////////////////////////////////////////////////
        /// ConditionNodeBase: override
        protected override bool Inner_ConditionCheck()
        {
            if (mCfg?.CndFunc != null)
            {
                return mCfg.CndFunc(this);
            }

            return false;
        }
    }
}