namespace Xease.CoreGame
{
    /// <summary>
    /// GameMode 自定义逻辑的运行时初始化信息：携带 ECWorlds / WorldCreationInfo，并向黑板写入世界上下文。
    /// </summary>
    public class GameModeGenInfo : CustomLogicGenInfo, IHasLogicWorld, IHasMetaWorld
    {
        // 所属世界集合，用于推导 LogicWorld / MetaWorld
        public ECWorlds ECWorlds { get; protected set; }
        // 世界创建参数（写入 CV_WorldInfo）
        public WorldCreationInfo WorldCreationInfo { get; protected set; }

        public LogicWorld LogicWorld => ECWorlds.LogicWorld;
        public MetaWorld MetaWorld => ECWorlds.MetaWorld;
        
        public override void Destroy()
        {
            ECWorlds = null;
            WorldCreationInfo = null;
            base.Destroy();
        }

        internal void Init(ECWorlds worlds, WorldCreationInfo worldCreationInfo)
        {
            if (worlds == null)
                G.LogError("GameModeGenInfo Init 异常, ECWorlds 为空");
            if (worldCreationInfo == null)
                G.LogError("GameModeGenInfo Init 异常, WorldCreationInfo 为空");

            ECWorlds = worlds;
            WorldCreationInfo = worldCreationInfo;
        }

        // 黑板 key 已存在时跳过写入并打错误日志
        protected void WriteVarIfAbsent<T>(ref VarEnv varEnv, string key, T value)
        {
            if (!varEnv.HasVar<T>(key))
                varEnv.WriteVar<T>(key, value);
            else
                G.LogError($"GameModeGenInfo CopyToPreVarEnv 出现异常, 外部有冗余 Key={key}");
        }

        public override VarEnv CopyToPreVarEnv(ref VarEnv varEnv)
        {
            if (ECWorlds == null)
            {
                G.LogError("GameModeGenInfo CopyToPreVarEnv 异常, 未调用 Init, ECWorlds 为空");
                return base.CopyToPreVarEnv(ref varEnv);
            }

            WriteVarIfAbsent(ref varEnv, CvKey.CV_WorldInfo, WorldCreationInfo);
            WriteVarIfAbsent(ref varEnv, CvKey.CV_LogicWorld, ECWorlds.LogicWorld);
            WriteVarIfAbsent(ref varEnv, CvKey.CV_MetaWorld, ECWorlds.MetaWorld);
            return base.CopyToPreVarEnv(ref varEnv);
        }

        /// <summary>
        /// 从对象池创建并初始化 GameMode 所需的 GenInfo。
        /// </summary>
        internal static GameModeGenInfo New(ICustomLogicService svc, ECWorlds worlds, WorldCreationInfo worldCreationInfo)
        {
            var genInfo = svc.NewGenInfo<GameModeGenInfo>();
            genInfo.Init(worlds, worldCreationInfo);
            return genInfo;
        }
        
    }
}
