using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;
    // using Logic = CustomLogicCfg;
    // using Templete = LogicTempleteCfg;
    // using CustomState = CustomBhvStateCfg;
    // using FSM = FSMNodeCfg;
    // using State = StateNodeCfg;
    // using Bhv = NoneParamBhvCfg;
    // using Log = LogBhvCfg;
    // using Seq = SequenceBhvCfg;         //顺序
    // using Parallel = ParallelBhvCfg;    //并行
    // using Delay = FTDelayBhvCfg;
    
    public class LogicConfigs_UnitTest : LogicConfigBase
    {
        public LogicConfigs_UnitTest(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(CustomLogic);
            InitConfigs_Test();
        }

        private void InitConfigs_Test()
        {
            AddConfig(9990001, new Nodes()
            {
                Log("LogicConfig_UnitTest 9990001"),
            });
            
            
        }
        
    }
}