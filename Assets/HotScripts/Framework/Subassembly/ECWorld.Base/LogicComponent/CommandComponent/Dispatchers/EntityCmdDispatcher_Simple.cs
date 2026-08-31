using Entitas;

namespace Xease.CoreGame
{
    public delegate bool ComponentHandleCommand(LogicEntity entity, EntityCommand cmd);

    public class EntityCmdDispatcher_Simple : IEntityCommandDispatcher
    {
        protected event ComponentHandleCommand OnHandleCommand;
        protected LogicEntity _owner;

        public virtual bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            if (OnHandleCommand != null)
            {
                OnHandleCommand(entity, cmd);
            }

            return true;
        }

        public virtual void BindOwner(LogicEntity owner)
        {
            _owner = owner;
            // 稀疏槽扫描，避免 GetComponents 冷缓存 ToArray 分配
            for (int i = 0, n = owner.totalComponents; i < n; i++)
            {
                if (!owner.HasComponent(i))
                    continue;
                var component = owner.GetComponent(i);
                if (component is IEntityCommandHandler commandHandler)
                {
                    OnHandleCommand += commandHandler.HandleEntityCommand;
                }
            }

            // GC警示：owner.OnComponent* 在 BindOwner 时已有 Context 订阅，+= 必走 Delegate.CombineImpl，每次新建 multicast（约 176B×2）；缓存 method group 挡不住
            owner.OnComponentAdded += _onComponentAdded;
            owner.OnComponentRemoved += _onComponentRemoved;
        }

        public virtual void UnBindOwner()
        {
            _owner.OnComponentAdded -= _onComponentAdded;
            _owner.OnComponentRemoved -= _onComponentRemoved;
            _owner = null;
            OnHandleCommand = null;
        }

        private void _onComponentAdded(IEntity entity, int index, IComponent component)
        {
            if (component is IEntityCommandHandler commandHandler)
            {
                OnHandleCommand += commandHandler.HandleEntityCommand;
            }
        }

        private void _onComponentRemoved(IEntity entity, int index, IComponent component)
        {
            if (component is IEntityCommandHandler commandHandler)
            {
                OnHandleCommand -= commandHandler.HandleEntityCommand;
            }
        }
    }
}