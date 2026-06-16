using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfig_Buff : LogicConfigBase
    {
        public LogicConfig_Buff(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(BuffLogic);
            
            //Buff: ID从 7000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddConfig(7100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试Buff模版 7100000");
                }),
            });
        }
    }
}
