namespace Xease.CoreGame
{
    public class LogicGameMode : CustomLogic
    {
        public GameLevelLogic LevelLogic { get; protected set; }
        
        public void SetLevelLogic(GameLevelLogic gameLevelLogic)
        {
            LevelLogic = gameLevelLogic;
        }
        
        public override void Destroy()
        {
            if (LevelLogic != null)
            {
                G.CustomLogic.DestroyLogic(LevelLogic);
                LevelLogic = null;
            }
            base.Destroy();
        }
    }
}
