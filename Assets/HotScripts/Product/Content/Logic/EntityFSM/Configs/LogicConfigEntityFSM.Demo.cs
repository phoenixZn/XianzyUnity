using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;
    
    public partial class LogicConfig_EntityFSM
    {
        //Demo、Test从3900001开始
        protected void InitConfigs_Demo()
        {
            AddConfig(3900001, new List<ICustomNodeCfg>()
            {
                //LogDebug(n=>{ n.Log("EntityMainFSM DemoEntity"); }),
                FSM<MainFSMNode>("MST_Born", new()
                {
                    MainState<DemoStateBorn>("MST_Born"),
                    MainState<DemoStateIdle>("MST_Idle"),
                    MainState<DemoStateMove>("MST_Move"),
                    MainState<DemoStateDie>("MST_Die"),
                }),
            }).DefaultVar((env) =>
            {
            });
        }
    }
}
