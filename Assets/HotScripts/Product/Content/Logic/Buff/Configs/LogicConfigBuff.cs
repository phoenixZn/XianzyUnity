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

            InitAttachPolicies();
            //Buff: ID从 7000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            AddBuff(7100000, new List<ICustomNodeCfg>()
            {
                InitializeCall((CustomNode node) =>
                {
                    G.Log("测试Buff模版 7100000");
                }),
            }).DefaultVar(env =>
            {
            });
        }

        /// <summary>
        /// 登记一个 Buff 配置（产出 <see cref="BuffCfg"/>，满足 BuffCenterComponent 的 IEntityBuffCfg 校验）。
        /// 返回的 cfg 可继续链式 WithBuffTag / WithTargetChecker / WithMergeRule。
        /// </summary>
        protected BuffCfg AddBuff(int id, Nodes nodes, string desc = null)
        {
            var logicCfg = new BuffCfg(id, nodes, DefaultLogicType, desc).WithDuration(CvKey.CV_BuffDuration);
            Add(id, logicCfg);
            return logicCfg;
        }
        
        protected BuffCfg AddBuff(int id, Nodes nodes, float buffDuration, string desc = null)
        {
            var logicCfg = new BuffCfg(id, nodes, DefaultLogicType, desc).WithDuration(CvKey.CV_BuffDuration, buffDuration);
            Add(id, logicCfg);
            return logicCfg;
        }
    }
}
