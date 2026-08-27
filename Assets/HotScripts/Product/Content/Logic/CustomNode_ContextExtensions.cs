
namespace Xease.CoreGame
{
    public static class CustomNodeContextExtensions
    {
        /// <summary>
        /// 当前逻辑节获取: LogicWorld。
        /// </summary>
        public static LogicWorld GetLogicWorld(this CustomNode self)
        {
            //标准先用GetGenInfo获取
            var genInfo = self.GetGenInfo<IHasLogicWorld>(false);
            if (genInfo != null)
            {
                return genInfo.LogicWorld;
            }
            //老代码的保底适配，从黑板固定Key取
            var key = CvKey.CV_LogicWorld;
            var world = self.GetVar<LogicWorld>(key);
            if (world == null)
            {
                self.LogError("GetLogicWorld world == null");
            }
            return world;
        }

        /// <summary>
        /// 当前逻辑节获取: MetaWorld。
        /// </summary>
        public static MetaWorld GetMetaWorld(this CustomNode self)
        {
            //标准先用GetGenInfo获取
            var genInfo = self.GetGenInfo<IHasMetaWorld>(false);
            if (genInfo != null)
            {
                return genInfo.MetaWorld;
            }
            //老代码的保底适配，从黑板固定Key取
            var metaWorld = self.GetVar<MetaWorld>(CvKey.CV_MetaWorld);
            if (metaWorld == null)
            {
                self.LogError("GetMetaWorld metaWorld == null");
            }
            return metaWorld;
        }


        /// <summary>
        /// 当前逻辑节获取: 拥有者实体。
        /// </summary>
        public static LogicEntity GetOwnerEntity(this CustomNode self)
        {
            //标准先用GetGenInfo获取
            var genInfo = self.GetGenInfo<IHasOwnerEntity>(false);
            if (genInfo != null)
            {
                return genInfo.OwnerEntity;
            }
            //老代码的保底适配，从黑板固定Key取
            var owner = self.GetVar<LogicEntity>(CvKey.CV_OwnerEntity);
            if (owner == null)
            {
                self.LogError("GetOwnerEntity owner == null");
            }
            return owner;
        }
        
        /// <summary>
        /// 当前逻辑节获取: 玩家信息。
        /// </summary>
        public static InGamePlayerInfo GetInGamePlayerInfo(this CustomNode self)
        {
            //标准先用GetGenInfo获取
            var genInfo = self.GetGenInfo<IHasOwnerPlayerInfo>(false);
            if (genInfo != null)
            {
                return genInfo.OwnerPlayerInfo;
            }
            //老代码的保底适配，从黑板固定Key取
            var playerInfo = self.GetVar<InGamePlayerInfo>(CvKey.CV_OwnerPlayerInfo);
            if (playerInfo == null)
            {
                self.LogError("node.GetOwnerBattlePlayerInfo == null");
            }
            return playerInfo;
        }

    }
}
