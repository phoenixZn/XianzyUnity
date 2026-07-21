
namespace Xease.CoreGame
{
    public static class CustomNodeContextExtensions
    {
        /// <summary>
        /// 获取当前逻辑节点黑板中的 LogicWorld。
        /// </summary>
        public static LogicWorld GetLogicWorld(this CustomNode self)
        {
            var key = CvKey.CV_LogicWorld;
            var world = self.GetVar<LogicWorld>(key);
            if (world == null)
            {
                self.LogError("GetLogicWorld world == null");
            }

            return world;
        }

        /// <summary>
        /// 获取当前逻辑节点黑板中的 MetaWorld。
        /// </summary>
        public static MetaWorld GetMetaWorld(this CustomNode self)
        {
            var metaWorld = self.GetVar<MetaWorld>(CvKey.CV_MetaWorld);
            if (metaWorld == null)
            {
                self.LogError("GetMetaWorld metaWorld == null");
            }

            return metaWorld;
        }

        /// <summary>
        /// 获取当前逻辑节点黑板中记录的拥有者实体。
        /// </summary>
        public static LogicEntity GetOwnerEntity(this CustomNode self)
        {
            var owner = self.GetVar<LogicEntity>(CvKey.CV_OwnerEntity);
            if (owner == null)
            {
                self.LogError("GetOwnerEntity owner == null");
            }
            return owner;
        }

        /// <summary>
        /// 获取当前逻辑节点黑板中的拥有者玩家信息。
        /// </summary>
        public static InGamePlayerInfo GetInGamePlayerInfo(this CustomNode self)
        {
            var playerInfo = self.GetVar<InGamePlayerInfo>(CvKey.CV_OwnerPlayerInfo);
            if (playerInfo == null)
            {
                self.LogError("node.GetInGamePlayerInfo == null");
            }
            return playerInfo;
        }

    }
}
