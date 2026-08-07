using System.Linq;
using Entitas;

namespace Xease.CoreGame
{

    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComUnityObjectRelated
    public static partial class WorldExtension
    {
        public static void AddEntityIndex_UnityObjectRelated(this LogicWorld world)
        {
            var index = new PrimaryEntityIndex<LogicEntity, int>(
                "EntityIndex_UnityObjectRelated",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComUnityObjectRelated)),
                (e, c) => ((UnityObjectRelatedComponent) c).gameObjectInstanceID.Keys.ToArray());
            world.AddEntityIndex(index);
        }

        public static LogicEntity GetEntityWithUnityObjectRelated(this LogicWorld world, int gameObjectInstanceID)
        {
            var index = world.GetEntityIndex("EntityIndex_UnityObjectRelated") as PrimaryEntityIndex<LogicEntity, int>;
            if (index == null)
            {
                return null;
            }
            return index.GetEntity(gameObjectInstanceID);
        }
        
    }
}
