namespace Xease.CoreGame
{
    //本Entity被主人Entity所持有
    public class HolderEntityComponent : LogicComponent
    {
        //持有者
        public long HolderEntityID { get; set; }

        public LogicEntity HolderEntity
        {
            get
            {
                if (_hostEntity == null || _hostEntity.OwnerWorld == null)
                {
                    return null;
                }
                return _hostEntity.OwnerWorld.GetEntity(HolderEntityID);
            }
        }
    }

    public partial class LogicEntity
    {
        public HolderEntityComponent comHolder
        {
            get { return (HolderEntityComponent)GetComponent(LogicComponentsLookup.ComHolderEntity); }
        }

        public bool hasComHolder
        {
            get { return HasComponent(LogicComponentsLookup.ComHolderEntity); }
        }
        
        public void SetHolderEntity(long entityID)
        {
            var index = LogicComponentsLookup.ComHolderEntity;
            var component = (HolderEntityComponent)CreateComponent(index, typeof(HolderEntityComponent));
            component.HolderEntityID = entityID;
            ReplaceComponent(index, component);
        }
    }

    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComHolderEntityIndex = new(typeof(HolderEntityComponent));
        public static int ComHolderEntity => _ComHolderEntityIndex.Index;
    }
}