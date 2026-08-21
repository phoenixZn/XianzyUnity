using System.Xml;

namespace Xease.CoreGame
{
    //////////////////////////////////////////////////////////////////////////
    public static partial class CLogger
    {
        public static bool LogAssert(XmlNode cfgNode, bool condition, string logMsg = null)
        {
            if (condition)
                return true;
            cfgNode.LogError(logMsg);
            return false;
        }

        public static void LogError(this XmlNode cfgNode, string logMsg)
        {
            int id = cfgNode.GetSingleNodeID();
            CLogger.LogError($"CLHelper XmlNode ParseError id={id} : {logMsg}");
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class CLHelper
    {
        /// XmlNode Helper
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
                CLogger.LogAssert(false, "CustomLogicConfig CreateNodeCfg ParseError cusNode as XmlElement == null");
                return null;
            }

            string nodeTypeStr = string.Format("{0}{1}", cusNode.GetAttribute("type"), "Cfg");
            ICustomNodeCfg nodeCfg = NodeConfigTypeRegistry.CreateCustomNodeCfg(nodeTypeStr);
            if (nodeCfg == null)
            {
                CLogger.LogAssert(false, "NodeConfigTypeRegistry.CreateCustomNodeCfg == null  nodeTypeStr = " + nodeTypeStr);
                return null;
            }

            var xmlNodeCfg = nodeCfg as IParseFromXml;
            if (xmlNodeCfg != null)
            {
                if (!xmlNodeCfg.ParseFromXml(node))
                {
                    node.LogError(nodeTypeStr);
                }
            }

            CLogger.LogAssert(nodeCfg != null);
            return nodeCfg;
        }
    }
}