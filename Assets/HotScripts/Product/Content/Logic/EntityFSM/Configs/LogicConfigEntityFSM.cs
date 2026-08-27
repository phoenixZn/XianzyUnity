using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    public class MainFSMNode : FSMNode, IEntityCommandHandler
    {
        public bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            if (cmd.CmdType == EntityCmdType.Nt_ForceDeath)
                TransToState("MST_Die");
            return false;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicConfig_EntityFSM : LogicConfigBase
    {
        public LogicConfig_EntityFSM(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(EntityMainFSMLogic);
            
            //实体状态机: ID从 3000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        //模板从3100000开始
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


        //////////////////////////////////////////////////////////////////////////
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StateNodeCfg MainState<T>(string stateID) where T : MainStateBase
        {
            return new StateNodeCfg() { StateID = stateID, StateClass = typeof(T) };
        }
    }
}
