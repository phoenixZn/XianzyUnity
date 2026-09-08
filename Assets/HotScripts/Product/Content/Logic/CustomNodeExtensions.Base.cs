
namespace Xease.CoreGame
{
    public static class CustomNodeContextExtensions
    {
        /// <summary>
        /// 当前逻辑节获取: LogicWorld。
        /// </summary>
        public static LogicWorld GetLogicWorld(this CustomNode self)
        {
            var genInfo = self.GetGenInfo<IHasLogicWorld>(false);
            if (genInfo != null)
            {
                return genInfo.LogicWorld;
            }
            self.LogError("node.GetLogicWorld world == null");
            return null;
        }

        /// <summary>
        /// 当前逻辑节获取: MetaWorld。
        /// </summary>
        public static MetaWorld GetMetaWorld(this CustomNode self)
        {
            var genInfo = self.GetGenInfo<IHasMetaWorld>(false);
            if (genInfo != null)
            {
                return genInfo.MetaWorld;
            }
            self.LogError("node.GetMetaWorld metaWorld == null");
            return null;
        }


        /// <summary>
        /// 当前逻辑节获取: 拥有者实体。
        /// </summary>
        public static LogicEntity GetOwnerEntity(this CustomNode self)
        {
            var genInfo = self.GetGenInfo<IHasOwnerEntity>(false);
            if (genInfo != null)
            {
                return genInfo.OwnerEntity;
            }
            self.LogError("node.GetOwnerEntity owner == null");
            return null;
        }
        
        /// <summary>
        /// 当前逻辑节获取: 玩家信息。
        /// </summary>
        public static InGamePlayerInfo GetOwnerPlayerInfo(this CustomNode self)
        {
            var genInfo = self.GetGenInfo<IHasOwnerPlayerInfo>(false);
            if (genInfo != null)
            {
                return genInfo.OwnerPlayerInfo;
            }
            self.LogError("node.GetOwnerPlayerInfo == null");
            return null;
        }

    }
}
