

namespace Xease.CoreGame
{
    /// <summary>
    /// CustomLogic相关一些调试、辅助代码
    /// </summary>
    public static partial class CLHelper
    {
        public static void AssertBreak()
        {
            CLogger.LogError("KaHotUpdate.CoreGameLogic Has ERROR! ");
            //UnityEngine.Debug.Break();
        }

        public static bool Assert(bool condition, object logMsg = null)
        {
            if (condition)
                return true;
            if (logMsg != null)
            {
                CLogger.LogError(logMsg.ToString());
            }

            AssertBreak();
            return false;
        }

        /// Node Helper
        public static void LogError(this CustomNode node, string logMsg)
        {
            int id = node.GenInfo.LogicConfigID;
            CLogger.LogError($"LogicNodeError id={id} : {logMsg}");
        }

        public static void LogInfo(this CustomNode node, string logMsg)
        {
            int id = node.GenInfo.LogicConfigID;
            CLogger.LogInfo($"Logic[ {id} ]({node.CreationIndex}): {logMsg}");
        }

        public static bool IsNodeCanStop(this CustomNode node)
        {
            if (node != null && node is INeedStopCheck check)
            {
                return check.CanStop();
            }

            return true;
        }

        public static void AssertNodeCfgCategory(ICustomNodeCfg nodeCfg, NodeCategory targetCategory, bool checkNull = true)
        {
            if (nodeCfg != null)
            {
                var category = NodeConfigTypeRegistry.GetNodeCfgCategory(nodeCfg.GetType());
                CLHelper.Assert(category == targetCategory);
            }
            else if (checkNull)
            {
                CLogger.LogError("LogicError nodeCfg == null");
            }
        }




    }
}