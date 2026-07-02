using System.Collections.Generic;
using System.Linq;
using Entitas;

namespace Xease.CoreGame
{
    public enum GameObjectType
    {
        None,
        Self,
    }

    public class UnityObjectRelatedComponent : LogicComponent
    {
        public Dictionary<int, GameObjectType> gameObjectInstanceID;

        public GameObjectType GetGameObjectType(int id)
        {
            if (gameObjectInstanceID.TryGetValue(id, out var type))
            {
                return type;
            }

            return GameObjectType.None;
        }

        public override void DisposeOnRemove()
        {
            base.DisposeOnRemove();

            gameObjectInstanceID.Clear();
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public UnityObjectRelatedComponent comUnityObjectRelated
        {
            get { return (UnityObjectRelatedComponent)GetComponent(LogicComponentsLookup.ComUnityObjectRelated); }
        }

        public bool hasComUnityObjectRelated
        {
            get { return HasComponent(LogicComponentsLookup.ComUnityObjectRelated); }
        }

        public void ReplaceComUnityObjectRelated(Dictionary<int, GameObjectType> newGameObjectInstanceID)
        {
            var index = LogicComponentsLookup.ComUnityObjectRelated;
            var component = (UnityObjectRelatedComponent)CreateComponent(index, typeof(UnityObjectRelatedComponent));
            component.gameObjectInstanceID = newGameObjectInstanceID;
            ReplaceComponent(index, component);
        }

        public void RemoveComUnityObjectRelated()
        {
            RemoveComponent(LogicComponentsLookup.ComUnityObjectRelated);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComUnityObjectRelated
    public static partial class WorldExtension
    {
        public static void AddEntityIndex_UnityObjectRelated(this LogicWorld world)
        {
            var index = new GroupEntityIndex<LogicEntity, int>(
                "EntityIndex_UnityObjectRelated",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComUnityObjectRelated)),
                (e, c) => ((UnityObjectRelatedComponent) c).gameObjectInstanceID.Keys.ToArray());
            world.AddEntityIndex(index);
        }

        public static LogicEntity GetEntityWithUnityObjectRelated(this LogicWorld world, int gameObjectInstanceID)
        {
            var index = world.GetEntityIndex("EntityIndex_UnityObjectRelated") as PrimaryEntityIndex<LogicEntity, long>;
            if (index == null)
            {
                return null;
            }
            return index.GetEntity(gameObjectInstanceID);
        }
        
    }
    
    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComUnityObjectRelatedIndex = new ComponentTypeIndex(typeof(UnityObjectRelatedComponent));
        public static int ComUnityObjectRelated => _ComUnityObjectRelatedIndex.Index;
    }
}
