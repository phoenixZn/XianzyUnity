using System.Collections.Generic;
using Entitas;

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

    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComHolderEntity
    public static partial class WorldExtension
    {
        public static void AddEntityIndex_ComHolderEntity(this LogicWorld world)
        {
            var index = new EntityIndex<LogicEntity, long>(
                "EntityIndex_HolderEntity",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComHolderEntity)),
                (e, c) => ((HolderEntityComponent)c).HolderEntityID);
            world.AddEntityIndex(index);
        }

        public static HashSet<LogicEntity> GetEntitiesWithComOwnerEntity(this LogicWorld world, long ownerEntityID)
        {
            var index = world.GetEntityIndex("EntityIndex_HolderEntity") as EntityIndex<LogicEntity, long>;
            if (index == null)
            {
                return null;
            }
            return index.GetEntities(ownerEntityID);
        }
    }

    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComHolderEntityIndex = new(typeof(HolderEntityComponent));
        public static int ComHolderEntity => _ComHolderEntityIndex.Index;
    }
}