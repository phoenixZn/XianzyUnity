using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfig_Subobject : LogicConfigBase
    {
        public LogicConfig_Subobject(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(SubobjectLogic);
            
            //子物体: ID从 6000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddConfig(6100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试Subobject模版 6100000");
                }),
            });
        }
    }
}
