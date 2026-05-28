using Entitas;
using System.Collections.Generic;

namespace HotUpdate.CoreGame
{
    public static partial class WorldExtension
    {
        
        public static void AddEntityIndex_ComID(this LogicWorld world)
        {
            var index = new PrimaryEntityIndex<LogicEntity, long>(
                "EntityIndex_ID",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComID)), 
                (e, c) => ((IDComponent) c).id);
            world.AddEntityIndex(index);
        }
        
        public static LogicEntity GetEntityWithComID(this LogicWorld world, long id)
        {
            var index = world.GetEntityIndex("EntityIndex_ID") as PrimaryEntityIndex<LogicEntity, long>;
            if (index == null)
            {
                return null;
            }
            return index.GetEntity(id);
        }
        
        
        public static HashSet<LogicEntity> GetEntitiesWithComTag(this LogicWorld world, uint tag)
        {
            return world.GetEntityIndex<TagComponent, EntityIndex<LogicEntity, uint>>().GetEntities(tag);
        }
        
        
        public static HashSet<LogicEntity> GetEntitiesWithComOwnerEntity(this LogicWorld world, long ownerEntityID)
        {
            return world.GetEntityIndex<HolderEntityComponent, EntityIndex<LogicEntity, long>>().GetEntities(ownerEntityID);
        }
    }
}