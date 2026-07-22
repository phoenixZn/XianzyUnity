namespace Xease.CoreGame
{
    public class EntityMainFSMLogic : EntityCmdLogic
    {
        // 主状态机；约定为 Logic 下第1个 FSMNode
        public FSMNode MainFsmNode { get; protected set; } 
        
        public override void InitializeNode(ICustomNodeCfg cfg, in CustomNodeContext context)
        {
            if (!(context.GenInfo is MainFsmGenInfo))
            {
                G.Log($"EntityMainFSMLogic InitializeNode GenInfo is not MainFsmGenInfo, GenInfo={context.GenInfo.GetType()}");
            }
            
            base.InitializeNode(cfg, context);
            G.Log($"MainFSM LogicConfigID={context.GenInfo.LogicConfigID}");
        }

        public override void Destroy()
        {
            MainFsmNode = null;
            base.Destroy();
        }

        protected override void CacheInterface(CustomNode node)
        {
            base.CacheInterface(node);
            if (node is FSMNode fsmNode)
            {
                if (MainFsmNode == null)
                    MainFsmNode = fsmNode;
                else
                    this.Log("EntityMainFSMLogic 有配置多个状态机");
            }
        }
    }
}
