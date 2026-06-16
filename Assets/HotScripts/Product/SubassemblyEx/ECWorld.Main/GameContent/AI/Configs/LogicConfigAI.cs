using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfig_AI : LogicConfigBase
    {
        public LogicConfig_AI(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(AILogic);
            
            //AI: ID从 5000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddConfig(5100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试AI模版 5100000");
                }),
            });
        }
    }
}
