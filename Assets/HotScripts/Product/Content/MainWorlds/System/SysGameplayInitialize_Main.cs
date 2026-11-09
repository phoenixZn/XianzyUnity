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
            SetupSubobjects();
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
            var genInfo = GameModeGenInfo.New(G.CustomLogic, _worlds, _worlds.CreationInfo);
            genInfo.LogicConfigID = modeParam.ModeLogicID;
            genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_GameMode;
            //genInfo.PreEnv = varEnv;
            _metaWorld.SetComUniGameMode(genInfo);
        }

        // 子物体仓库由先于本系统初始化的 SysSubobject 创建，这里只补项目侧配置
        protected void SetupSubobjects()
        {
            if (!_metaWorld.hasComUniSubobjects)
                return;
            _metaWorld.comUniSubobjects.DefaultLogicContainer = LogicContainerKey.LogicConfigs_Subobject;
            DemoSubobject.InstallHitSink(_metaWorld.comUniSubobjects);
        }
    }
}
