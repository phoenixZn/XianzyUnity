using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfig_Supply : LogicConfigBase
    {
        public LogicConfig_Supply(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(SupplyLogic);
            
            //Supply: ID从 8000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddConfig(8100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试Supply模版 8100000");
                }),
            });
        }
    }
}
