using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{

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
}
