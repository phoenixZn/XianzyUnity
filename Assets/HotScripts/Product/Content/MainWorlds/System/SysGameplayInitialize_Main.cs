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
            G.Log("SysGameplayInitialize_Main InitEntityIndex");
        }
        
        protected override void AddMetaComponents()
        {
            G.Log("SysGameplayInitialize_Main AddMetaComponents");
            AddMeta_GameMode();
        }

        protected override void RemoveMetaComponents()
        {
            G.Log("SysGameplayInitialize_Main RemoveMetaComponents");
            _metaWorld.RemoveComUniGameMode();
        }

        //////////////////////////////////////////////////////////////////////////
        protected void AddMeta_GameMode()
        {
            var modeParam = _worlds.GetCreationInfo<IGameModeParam>();
            if (modeParam == null)
            {
                G.LogError("modeParam == null");
            }
            G.Log($"AddMeta_GameMode ModeLogicID={modeParam.ModeLogicID}");
            var genInfo = G.CustomLogic.NewGenInfo<CustomLogicGenInfo>();
            genInfo.LogicConfigID = modeParam.ModeLogicID;
            genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_GameMode;
            //genInfo.PreEnv = varEnv;
            _metaWorld.SetComUniGameMode(genInfo);
        }
    }
}