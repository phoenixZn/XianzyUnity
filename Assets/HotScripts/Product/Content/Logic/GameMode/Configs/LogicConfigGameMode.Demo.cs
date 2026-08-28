using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfigs_GameMode
    {
        private void InitConfigs_Demo()
        {
            //////////////////////////////////////////////////////////////////////////
            //Demo GameMode状态机:
            AddConfig(1900001, new Nodes()
            {
                LogDebug(n=>{ n.Log("Demo GameMode状态机"); }),
                FSM("GST_Loading", new() 
                {
                    CustomState("GST_Loading", "GST_InitGame",Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("DemoMode Loading")),
                    })),
                    
                    CustomState("GST_InitGame", "GST_Playing",Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("DemoMode InitGame")),
                    })),
                    
                    CustomState<DemoModePlaying>("GST_Playing", Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("MainMode Playing")),
                    })),
                    
                    CustomState("GST_Pause", Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("MainMode Pause")),
                    })),
                }),
                Bhv<DemoTouchInputBhv>(),
                
            }).DefaultVar(env =>
            {
            });
        }
    }
}