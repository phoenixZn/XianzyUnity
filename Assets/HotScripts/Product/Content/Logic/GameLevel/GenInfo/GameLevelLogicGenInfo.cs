namespace Xease.CoreGame
{
    /// <summary>
    /// GameLevelLogic自定义逻辑的运行时初始化信息
    /// </summary>
    public class GameLevelLogicGenInfo : CustomLogicGenInfo, IHasLogicWorld, IHasMetaWorld
    {
        public ECWorlds ECWorlds { get; protected set; }
        public WorldCreationInfo WorldCreationInfo { get; protected set; }

        //////////////////////////////////////////////////////////////////////////
        /// IHasLogicWorld, IHasMetaWorld:
        public LogicWorld LogicWorld => ECWorlds.LogicWorld;
        public MetaWorld MetaWorld => ECWorlds.MetaWorld;

        //////////////////////////////////////////////////////////////////////////
        /// CustomLogicGenInfo:
        public override void Destroy()
        {
            ECWorlds = null;
            WorldCreationInfo = null;
            base.Destroy();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:
        internal void Init(ECWorlds worlds, WorldCreationInfo worldCreationInfo)
        {
            if (worlds == null)
                G.LogError("GameLevelLogicGenInfo Init 异常, ECWorlds 为空");
            if (worldCreationInfo == null)
                G.LogError("GameLevelLogicGenInfo Init 异常, WorldCreationInfo 为空");

            ECWorlds = worlds;
            WorldCreationInfo = worldCreationInfo;
        }
        
        /// <summary>
        /// 从对象池创建并初始化 GameMode 所需的 GenInfo。
        /// </summary>
        internal static GameLevelLogicGenInfo New(ICustomLogicService svc, ECWorlds worlds, WorldCreationInfo worldCreationInfo)
        {
            var genInfo = svc.NewGenInfo<GameLevelLogicGenInfo>();
            genInfo.Init(worlds, worldCreationInfo);
            return genInfo;
        }
        
    }
}