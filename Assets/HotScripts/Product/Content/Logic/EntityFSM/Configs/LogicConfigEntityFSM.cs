using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfig_EntityFSM : LogicConfigBase
    {
        public LogicConfig_EntityFSM(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(EntityFSMLogic);
            
            //实体状态机: ID从 3000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddConfig(3100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试EntityFSM模版 3100000");
                }),
            });
            
            AddConfig(3100001, new List<ICustomNodeCfg>()
            {
                Templete(3100000),
                LogDebug(n=>{ n.Log("测试Entity FSM");})
            });
        }
    }
}
