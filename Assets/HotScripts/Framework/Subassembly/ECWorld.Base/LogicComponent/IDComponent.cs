using Entitas;

namespace Xease.CoreGame
{
    public class IDComponent : LogicComponent
    {
        public long id { get; private set; }

        public void Init(long id)
        {
            this.id = id;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public IDComponent comID { get { return (IDComponent)GetComponent(LogicComponentsLookup.ComID); } }
        public bool hasComID { get { return HasComponent(LogicComponentsLookup.ComID); } }

        public void AddComID(long newId)
        {
            var index = LogicComponentsLookup.ComID;
            if (index < 0)
            {
                WLogger.LogError("AddComID 未初始化的组件索引 LogicComponentsLookup.ComID");
                return;
            }
            var component = (IDComponent)CreateComponent(index, typeof(IDComponent));
            component.Init(newId);
            AddComponent(index, component);
        }
        
        public long ID
        {
            get
            {
                if (hasComID)
                {
                    return comID.id;
                }
                return creationIndex;
            }
        } 
    }
    
    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComID
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

        public static LogicEntity GetEntity(this LogicWorld world, long id)
        {
            return world.GetEntityWithComID(id);
        }
    }
    

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComIDIndex = new (typeof(IDComponent));
        public static int ComID => _ComIDIndex.Index;
    }



}