using System.Xml;

namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool _AlwaysTrueCndCfg = Register(typeof(AlwaysTrueCndCfg), NodeCategory.Cnd);
    }

    public class AlwaysTrueCndCfg : ConditionBaseCfg
    {
        public override System.Type NodeType()
        {
            return typeof(AlwaysTrueCnd);
        }

        public override bool ParseFromXml(XmlNode cndNode)
        {
            return base.ParseFromXml(cndNode);
        }
    }

    public class AlwaysTrueCnd : ConditionNodeBase
    {
        protected override bool Inner_ConditionCheck()
        {
            return true;
        }
    }
}