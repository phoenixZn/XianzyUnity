namespace Xease.CoreGame
{
    public class MainWorldCreationInfo : WorldCreationInfo, IGameModeParam
    {
        public MainWorldCreationInfo() : base()
        {
        }
        public int ModeLogicID { get; set; }
    }
}