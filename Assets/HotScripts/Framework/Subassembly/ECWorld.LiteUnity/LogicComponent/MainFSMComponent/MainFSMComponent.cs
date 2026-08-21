using System.Collections.Generic;

namespace Xease.CoreGame
{
    public class EntityCmdLogic : CustomLogic, IEntityCommandHandler
    {
        protected List<IEntityCommandHandler> _entityCmdHandlerList = new();

        public virtual bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            for (int i = 0; i < _entityCmdHandlerList.Count; ++i)
            {
                var theNode = _entityCmdHandlerList[i];
                var node = theNode as ICustomNode;
                if (node != null && node.IsActive)
                {
                    if (theNode.HandleEntityCommand(entity, cmd))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        protected override void ClearInterfaceCache()
        {
            _entityCmdHandlerList.Clear();
            base.ClearInterfaceCache();
        }

        protected override void CacheInterface(CustomNode node)
        {
            base.CacheInterface(node);
            TraverseCollectInterface(ref _entityCmdHandlerList, node);
        }
    }


    

    public class MainFSMComponent : LogicComponent, IEntityCommandHandler
    {
        public EntityCmdLogic Logic { get; private set; }

        public override void DisposeOnRemove()
        {
            ClearLogic();
            base.DisposeOnRemove();
        }
        
        public void Init(EntityCmdLogic fsmLogic)
        {
            ClearLogic();
            Logic = fsmLogic;
        }

        private void ClearLogic()
        {
            if (Logic != null)
            {
                G.CustomLogic.DestroyLogic(Logic);
                Logic = null;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEntityCommandHandler:
        public bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            return Logic.HandleEntityCommand(entity, cmd);
        }
    }

    public partial class LogicEntity
    {
        public MainFSMComponent comFSM
        {
            get { return (MainFSMComponent)GetComponent(LogicComponentsLookup.ComMainFSM); }
        }

        public bool hasComFSM
        {
            get { return HasComponent(LogicComponentsLookup.ComMainFSM); }
        }

        public void AddComFSM(EntityCmdLogic logic)
        {
            if (logic == null)
            {
                G.LogError("AddComFSM logic == null");
                return;
            }
            var index = LogicComponentsLookup.ComMainFSM;
            var component = (MainFSMComponent)CreateComponent(index, typeof(MainFSMComponent));
            component.Init(logic);
            AddComponent(index, component);
        }

        public void RemoveComFSM()
        {
            if (hasComFSM)
            {
                RemoveComponent(LogicComponentsLookup.ComMainFSM);
            }
        }
    }


    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComMainFSMIndex = new(typeof(MainFSMComponent));
        public static int ComMainFSM => _ComMainFSMIndex.Index;
    }
}
