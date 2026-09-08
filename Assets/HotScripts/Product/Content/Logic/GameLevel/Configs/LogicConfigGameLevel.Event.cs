using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;
    
    public partial class LogicConfigs_GameLevelEvent : LogicConfigBase
    {
        public LogicConfigs_GameLevelEvent(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(CustomLogic);
            //关卡Event子逻辑ID从 2900000 开始
            InitConfigs_Template();
            InitConfigs();
        }

        private void InitConfigs_Template()
        {
        }
        
        private void InitConfigs()
        {
        }
    }
}