namespace Xease.CoreGame
{
    public partial class CustomLogicService
    {
        public void AddConfigContainers()
        {
            AddConfigContainer(new LogicConfigs_GameMode(LogicContainerKey.LogicConfigs_GameMode));
            AddConfigContainer(new LogicConfigs_GameLevel(LogicContainerKey.LogicConfigs_GameLevel));
            AddConfigContainer(new LogicConfigs_GameLevelEvent(LogicContainerKey.LogicConfigs_GameLevelEvent));
            AddConfigContainer(new LogicConfig_Skill(LogicContainerKey.LogicConfigs_Skill));
            AddConfigContainer(new LogicConfig_EntityFSM(LogicContainerKey.LogicConfigs_EntityFSM));
            AddConfigContainer(new LogicConfig_AI(LogicContainerKey.LogicConfigs_AI));
            AddConfigContainer(new LogicConfig_Subobject(LogicContainerKey.LogicConfigs_Subobject));
            AddConfigContainer(new LogicConfig_Buff(LogicContainerKey.LogicConfigs_Buff));
            AddConfigContainer(new LogicConfig_Supply(LogicContainerKey.LogicConfigs_Supply));
        }
    }
}