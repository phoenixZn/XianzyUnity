using Xease;

namespace Xease.CoreGame
{

    public class DemoModePlaying : CustomBhvState
    {
        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            base.InitializeNode(cfg, context);
            InnerClear();
        }

        public override void Destroy()
        {
            // Exit 未走到时也卸掉订阅，避免销毁后空格仍刷怪
            G.ValueEvent.RemoveHandler<EvtDemoSpacePressed>(OnEvtDemoSpacePressed);
            InnerClear();
            base.Destroy();
        }

        public override void Enter()
        {
            base.Enter();
            CreateDemoEnemies(10);
            G.ValueEvent.AddHandler<EvtDemoSpacePressed>(OnEvtDemoSpacePressed);
        }

        /// <summary>
        /// 离开 Playing 后不再响应空格刷怪。
        /// </summary>
        public override void Exit()
        {
            G.ValueEvent.RemoveHandler<EvtDemoSpacePressed>(OnEvtDemoSpacePressed);
            base.Exit();
        }

        public override float Update(float dt)
        {
            return base.Update(dt);
        }

        // 判断游戏胜利失败，要转到游戏结束状态
        public override string CheckTransitions()
        {
            var nextID = base.CheckTransitions();
            return nextID;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        private void InnerClear()
        {
        }

        // 与原 CreateDemoEntity_Enemys 相同：挂 OwnerPlayer + EntityFSM 3900001
        private void CreateDemoEnemies(int count)
        {
            InGamePlayerInfo playerInfo = null;
            if (GetGenInfo<GameModeGenInfo>().WorldCreationInfo is MainWorldCreationInfo mainWorldCreationInfo)
                playerInfo = mainWorldCreationInfo.LocalPlayer;
            if (playerInfo == null)
                this.LogError("playerInfo == null");

            for (int i = 0; i < count; i++)
            {
                LogicEntity entity = this.GetLogicWorld().CreateEntity();
                entity.AddComOwnerPlayer(playerInfo);

                var svc = G.CustomLogic;
                var genInfo = MainFsmGenInfo.New(svc,
                    metaWorld: this.GetMetaWorld(),
                    ownerEntity: entity);
                genInfo.LogicConfigID = 3900001;
                genInfo.ConfigContainerName = LogicContainerKey.LogicConfigs_EntityFSM;
                var logic = svc.CreateLogic<EntityMainFSMLogic>(genInfo);
                if (logic != null)
                    entity.AddComFSM(logic);
            }
        }

        // Playing 期间空格 → 补刷 1 个
        private void OnEvtDemoSpacePressed(EvtDemoSpacePressed evt)
        {
            CreateDemoEnemies(1);
        }
    }
}
