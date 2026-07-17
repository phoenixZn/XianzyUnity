using Entitas;

namespace Xease.CoreGame
{
    public class LogicComponent : IComponent, IComponentDispose
    {
        protected LogicEntity _hostEntity;
        public LogicEntity HostEntity  => _hostEntity;

        public virtual void PostInitialize(LogicEntity hostEntity)
        {
            _hostEntity = hostEntity;
        }

        public virtual void DisposeOnRemove()
        {
            _hostEntity = null;
        }

        public static bool operator !(LogicComponent component)
        {
            return component == null;
        }

        public static bool operator true(LogicComponent component)
        {
            return component != null;
        }

        public static bool operator false(LogicComponent component)
        {
            return component == null;
        }
    }
}