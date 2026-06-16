using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;
    
    public partial class LogicConfigs_GameLevel : LogicConfigBase
    {
        public LogicConfigs_GameLevel(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(CustomLogic);
            //游戏模式: ID从 2000000 开始
            InitConfigs_Template();
        }

        private void InitConfigs_Template()
        {
        }
        
    }
}