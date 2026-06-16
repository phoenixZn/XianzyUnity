using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfig_Skill : LogicConfigBase
    {
        public LogicConfig_Skill(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(SkillLogic);
            
            //技能: ID从 4000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddConfig(4100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试Skill模版 4100000");
                }),
            });
        }
    }
}