using Entitas;

namespace Xease.CoreGame
{
    public interface IGameModeParam
    {
        public int ModeLogicID { get; }
    }
    
    public class SysGameModeUpdate : ECWorldSystem, IUpdateSystem
    {
        public SysGameModeUpdate(ECWorlds world) : base(world)
        {
        }
        
        public void Update(float dt, float dt_unscaled)
        {
            var comUniGameMode = _metaWorld.comUniGameMode;
            if (comUniGameMode)
            {
                var modeLogic = comUniGameMode.GameModeLogic;
                modeLogic.Update(dt);
            }
        }
    }
}