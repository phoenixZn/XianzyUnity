using System.Collections.Generic;
using System.Xml;

namespace Xease.CoreGame
{
    public class NodeCfgList<T> : List<T> where T : class, ICustomNodeCfg
    {
        public bool ParseFromXml(XmlNode xmlNode, string xmlNodeName = "Node")
        {
            Clear();
            XmlNodeList subNodeList = xmlNode.SelectNodes(xmlNodeName);
            if (subNodeList == null)
                return false;
            foreach (XmlNode subNode in subNodeList)
            {
                var nodeCfg = CLHelper.CreateNodeCfg(subNode) as T;
                CLogger.LogAssert(nodeCfg != null);
                this.Add(nodeCfg);
            }

            if (this.Count == 0)
            {
                xmlNode.LogError("NodeCfgList.ParseFromXml() CfgList.Count == 0");
                CLogger.AssertBreak();
                return false;
            }

            return true;
        }
    }
}