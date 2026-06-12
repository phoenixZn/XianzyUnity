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
            foreach (var component in owner.GetComponents())
            {
                if (component is IEntityCommandHandler commandHandler)
                {
                    OnHandleCommand += commandHandler.HandleEntityCommand;
                }
            }

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