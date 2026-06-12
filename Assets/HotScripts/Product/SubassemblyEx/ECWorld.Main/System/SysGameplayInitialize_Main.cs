using Entitas;

namespace Xease.CoreGame
{
    public class SysGameplayInitialize_Main : InitializeSystem
    {
        public SysGameplayInitialize_Main(ECWorlds worlds) : base(worlds)
        {
        }
        
        protected override void InitEntityIndex()
        {

        }
        
        protected override void AddMetaComponents()
        {
            AddMeta_GameMode();
        }

        protected override void RemoveMetaComponents()
        {
            _metaWorld.RemoveComUniGameMode();
        }

        //////////////////////////////////////////////////////////////////////////
        protected void AddMeta_GameMode()
        {
            var gameModeParam = _worlds.GetCreationInfo<IGameModeParam>();
            var genInfo = G.CustomLogic.NewGenInfo<ICustomLogicGenInfo>();
            genInfo.LogicConfigID = gameModeParam.ModeLogicID;
            genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_GameMode;
            //genInfo.PreEnv = varEnv;
            _metaWorld.SetComUniGameMode(genInfo);
        }
    }
}