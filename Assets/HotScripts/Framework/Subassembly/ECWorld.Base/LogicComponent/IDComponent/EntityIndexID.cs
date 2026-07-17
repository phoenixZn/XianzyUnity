using System;
using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public class EntityIndex_Name : PrimaryEntityIndex<LogicEntity, string>
    {
        public EntityIndex_Name(string name, IGroup<LogicEntity> group, Func<LogicEntity, IComponent, string> getKey) : base(name, group, getKey){}
        public EntityIndex_Name(string name, IGroup<LogicEntity> group, Func<LogicEntity, IComponent, string[]> getKeys) : base(name, group, getKeys) { }
        public EntityIndex_Name(string name, IGroup<LogicEntity> group, Func<LogicEntity, IComponent, string> getKey, IEqualityComparer<string> comparer) : base(name, group, getKey, comparer) { }
        public EntityIndex_Name(string name, IGroup<LogicEntity> group, Func<LogicEntity, IComponent, string[]> getKeys, IEqualityComparer<string> comparer) : base(name, group, getKeys, comparer) { }
        protected override void addEntity(string key, LogicEntity entity)
        {
            if (string.IsNullOrEmpty(key))
                return;
            base.addEntity(key, entity);
        }
        protected override void removeEntity(string key, LogicEntity entity) {
            if (string.IsNullOrEmpty(key))
                return;
            base.removeEntity(key, entity);
        }
    }
    
    
    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComID
    public static partial class WorldExtension
    {
        //////////////////////////////////////////////////////////////////////////
        /// ID Number
        public static void AddEntityIndex_ComID(this LogicWorld world)
        {
            var index = new PrimaryEntityIndex<LogicEntity, long>(
                "EntityIndex_ID",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComID)),
                (e, c) => ((IDComponent) c).ID);
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


        //////////////////////////////////////////////////////////////////////////
        /// ID String
        public static void AddEntityIndex_Name(this LogicWorld world)
        {
            var index = new EntityIndex_Name(
                "EntityIndex_Name",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComID)),
                (e, c) => ((IDComponent) c).Name);
            world.AddEntityIndex(index);
        }

        public static LogicEntity GetEntityWithName(this LogicWorld world, string name)
        {
            var index = world.GetEntityIndex("EntityIndex_Name") as PrimaryEntityIndex<LogicEntity, string>;
            if (index == null)
            {
                return null;
            }
            return index.GetEntity(name);
        }
        
    }
}