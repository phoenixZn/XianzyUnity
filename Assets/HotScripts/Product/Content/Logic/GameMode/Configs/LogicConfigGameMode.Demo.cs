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
                        
                        BeginCall(CreateDemoEntity_Enemys),
                    })),
                    
                    CustomState<GameStatePlaying>("GST_Playing", Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("MainMode Playing")),
                    })),
                    
                    CustomState("GST_Pause", Seq(new Nodes()
                    {
                        LogDebug(n => n.Log("MainMode Pause")),
                    })),
                    
                }),
            }).DefaultVar(env =>
            {
            });
        }

        private static void CreateDemoEntity_Enemys(CustomNode n)
        {
            InGamePlayerInfo playerInfo = null;
            if (n.GetGenInfo<GameModeGenInfo>().WorldCreationInfo is MainWorldCreationInfo mainWorldCreationInfo)
            {
                playerInfo = mainWorldCreationInfo.LocalPlayer;
            }
            if (playerInfo == null)
                n.LogError("playerInfo == null");

            for (int i = 0; i < 10; i++)
            {
                LogicEntity entity = n.GetLogicWorld().CreateEntity();
                entity.AddComOwnerPlayer(playerInfo);

                var svc = G.CustomLogic;
                var genInfo = MainFsmGenInfo.New(svc,
                    metaWorld: n.GetMetaWorld(),
                    ownerEntity: entity);
                genInfo.LogicConfigID = 3900001;
                genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_EntityFSM;
                var logic = svc.CreateLogic<EntityMainFSMLogic>(genInfo);
                if (logic != null)
                {
                    entity.AddComFSM(logic);
                }
                
            }
        }
    }
}