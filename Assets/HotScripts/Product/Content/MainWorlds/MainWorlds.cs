namespace Xease.CoreGame
{
    public class MainWorldCreationInfo : WorldCreationInfo, IGameModeParam
    {
        public MainWorldCreationInfo() : base("Main")
        {
        }
        public int ModeLogicID { get; set; }
    }

    //////////////////////////////////////////////////////////////////////////
    /// 主游戏世界
    public class MainWorlds : LiteUnityWorlds
    {
    }
}