
namespace Xease.CoreGame
{
    
    ////////////////////////////////////////////////////////////////////////
    /// 主状态机：基础版本
    ////////////////////////////////////////////////////////////////////////
    public class MainFsmGenInfo : CustomLogicGenInfo, IHasLogicWorld, IHasMetaWorld, IHasOwnerEntity, IHasOwnerPlayerInfo
    {
        public LogicWorld LogicWorld { get; protected set; }
        public MetaWorld MetaWorld { get; protected set; }
        public LogicEntity OwnerEntity { get; protected set; }
        public InGamePlayerInfo OwnerPlayerInfo { get; protected set; }

        public override void Destroy()
        {
            LogicWorld = null;
            MetaWorld = null;
            OwnerEntity = null;
            OwnerPlayerInfo = null;
            base.Destroy();
        }

        internal void Init(MetaWorld metaWorld, LogicEntity ownerEntity)
        {
            if (metaWorld == null)
                G.LogError("MainFsmGenInfo Init 异常, MetaWorld 为空");
            if (ownerEntity == null)
                G.LogError("MainFsmGenInfo Init 异常, OwnerEntity 为空");

            var logicWorld = ownerEntity.OwnerWorld;
            if (logicWorld == null)
                G.LogError("MainFsmGenInfo Init 异常, LogicWorld 为空");

            var ownerFighterEntityID = ownerEntity.ID;
            if (ownerFighterEntityID == 0)
                G.LogError("MainFsmGenInfo Init 异常, OwnerFighterEntityID 为 0");

            LogicWorld = logicWorld;
            MetaWorld = metaWorld;
            OwnerEntity = ownerEntity;
            // OwnerPlayerInfo = ownerEntity.GetPlayerInfo();
            // if (OwnerPlayerInfo == null)
            //     G.LogError("MainFsmGenInfo Init 异常, ownerEntity.GetPlayerInfo() 为空 (推导获取失败)");
        }

        /// <summary>
        /// 从对象池创建并初始化非战斗单位主 FSM 所需的 GenInfo。
        /// </summary>
        internal static MainFsmGenInfo New(ICustomLogicService svc, MetaWorld metaWorld, LogicEntity ownerEntity)
        {
            var genInfo = svc.NewGenInfo<MainFsmGenInfo>();
            genInfo.Init(metaWorld, ownerEntity);
            return genInfo;
        }
    }
    
    
}
