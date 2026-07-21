using System.Xml;

namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool _LogicTempleteNodeCfg = Register(typeof(LogicTempleteCfg), NodeCategory.Mixture);
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// CustomLogic模板节点， 等价于在该LogicTempletNode处直接插入TempletLogic的全部节点
    /// </summary>
    public class LogicTempleteCfg : ICustomNodeCfg, IParseFromXml
    {
        public int LogicID { get; protected set; }

        public System.Type NodeType()
        {
            CLogger.LogError("ERROR : try to Initialize LogicTempleteNode!");
            return null;
        }

        public LogicTempleteCfg()
        {
        }

        public LogicTempleteCfg(int logicID)
        {
            LogicID = logicID;
        }

        public bool ParseFromXml(XmlNode xmlNode)
        {
            LogicID = XmlHelper.GetInt(xmlNode, "LogicID", -1);
            if (LogicID == -1)
            {
                return false;
            }

            return true;
        }
    }
}