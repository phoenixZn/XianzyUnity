using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;

    
    public partial class LogicConfigs_GameMode : LogicConfigBase
    {
        public LogicConfigs_GameMode(string name)
            : base(name, 20)
        {
            DefaultLogicType = typeof(CustomLogic);
            //游戏模式: ID从 1000000 开始
            InitConfigs_Template();
            InitConfigs_Demo();
        }

        private void InitConfigs_Template()
        {
            //////////////////////////////////////////////////////////////////////////
            //Main模式规则逻辑节点 （散装）
            AddConfig(1100000, new Nodes()
            {
                // Bhv<InitPlayerStatusBhv>(),
                // Bhv<HandleStartBattleTimeBhv>(),
                // new ChapterPlayerKillEnemyCntCtrlCfg(),
                // new ChapterPlayerInGameExpCtrlCfg(CvKey.CV_ExpCfg),
                // //结束相关
                // SaveEventToVar<EvtInGameSumUp>("CV_EvtInGameSumUp"),
                // SaveEventToVar<EvtGameLevelCompleted>("CV_EvtGameLevelCompleted"),
                //输入状态机：
                //InputFSMTemplate.CommonInputFSM,
            });
            
            //////////////////////////////////////////////////////////////////////////
            //Main模式标准状态机 模版:
            AddConfig(1100001, new Nodes()
            {
                FSM("GST_Loading", new() 
                {
                    CustomState("GST_Loading", "GST_InitGame",Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("GameMode Loading")),
                    })),
                    
                    CustomState("GST_InitGame", "GST_Playing",Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("GameMode InitGame")),
                    })),
                    
                    CustomState<GameModePlaying>("GST_Playing", Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("GameMode Playing")),
                    })),
                    
                    CustomState("GST_Pause", Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("GameMode Pause")),
                    })),
                    
                }),
            }).DefaultVar(env =>
            {
            });
        }
        
    }
}