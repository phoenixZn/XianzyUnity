using Entitas;

namespace Xease.CoreGame
{
    public class MetaComponent : IComponent, IComponentDispose
    {
        private MetaEntity _hostEntity;
        public MetaEntity HostEntity => _hostEntity;
        //public MetaEntity Owner => _hostEntity; //适配旧名字

        public virtual void PostInitialize(MetaEntity hostEntity)
        {
            _hostEntity = hostEntity;
        }

        public virtual void DisposeOnRemove()
        {
            _hostEntity = null;
        }

        public static bool operator !(MetaComponent component)
        {
            return component == null;
        }

        public static bool operator true(MetaComponent component)
        {
            return component != null;
        }

        public static bool operator false(MetaComponent component)
        {
            return component == null;
        }
    }
}