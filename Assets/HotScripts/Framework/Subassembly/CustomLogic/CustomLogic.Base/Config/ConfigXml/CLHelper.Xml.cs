using System.Xml;

namespace HotUpdate.CoreGame
{
    public static partial class CLHelper
    {
        /// XmlNode Helper
        public static bool Assert(XmlNode cfgNode, bool condition, string logMsg = null)
        {
            if (condition)
                return true;
            LogError(cfgNode, logMsg);
            return false;
        }

        public static void LogError(XmlNode cfgNode, string logMsg)
        {
            int id = cfgNode.GetSingleNodeID();
            CLogger.LogError($"CLHelper XmlNode ParseError id={id} : {logMsg}");
        }

        public static int GetSingleNodeID(this XmlNode cfgNode)
        {
            XmlNode node = cfgNode;
            while (node != null)
            {
                XmlNode idnode = node.SelectSingleNode("ID");
                if (idnode != null)
                {
                    int id = -1;
                    int.TryParse(idnode.InnerText, out id);
                    return id;
                }

                node = node.ParentNode;
            }

            CLogger.LogError("XmlNode GetSingleNodeID ERROR!");
            return -1;
        }

        public static ICustomNodeCfg CreateNodeCfg(XmlNode node)
        {
            if (node == null)
            {
                return null;
            }

            XmlElement cusNode = node as XmlElement;
            if (cusNode == null)
            {
                CLHelper.Assert(false, "CustomLogicConfig PraseNodeCfg ParseError  cusNode as XmlElement == null");
                return null;
            }

            string nodeTypeStr = string.Format("{0}{1}", cusNode.GetAttribute("type"), "Cfg");
            ICustomNodeCfg nodeCfg = NodeConfigTypeRegistry.CreateCustomNodeCfg(nodeTypeStr);
            if (nodeCfg == null)
            {
                CLHelper.Assert(false, "NodeConfigTypeRegistry.CreateCustomNodeCfg == null  nodeTypeStr = " + nodeTypeStr);
                return null;
            }

            var xmlNodeCfg = nodeCfg as IParseFromXml;
            if (xmlNodeCfg != null)
            {
                if (!xmlNodeCfg.ParseFromXml(node))
                {
                    CLHelper.LogError(node, nodeTypeStr);
                }
            }

            CLHelper.Assert(nodeCfg != null);
            return nodeCfg;
        }
    }
}