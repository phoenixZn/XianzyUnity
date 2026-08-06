using Entitas;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Xease.CoreGame
{
    public partial class WorldCreationInfo
    {
        public string WorldName { get; set; }
        public Type WorldsClassType { get; set; }

        public Func<WorldCreationInfo, ECWorlds, Systems> CreateRootSystems { get; set; }

        protected WorldCreationInfo()
        {
        }
    }

    public abstract class ECWorlds
    {
        //初始参数
        public WorldCreationInfo CreationInfo { get; protected set; }

        //逻辑世界（游戏实体）
        public LogicWorld LogicWorld { get; protected set; }
        
        //抽象世界（World组件）
        public MetaWorld MetaWorld { get; protected set; }
        
        //系统入口
        protected Systems _rootSystem;

        //////////////////////////////////////////////////////////////////////////
        /// 构造、销毁
        public virtual void InitWorlds(WorldCreationInfo creationInfo)
        {
            CreationInfo = creationInfo;

            //构造世界
            CreateMetaWorld();
            CreateLogicWorld();

            //构造系统
            CreateSystems();

            //初始化系统
            if (_rootSystem != null)
            {
                _rootSystem.ActivateReactiveSystems();
                _rootSystem.Initialize();
            }
        }

        public virtual void DestroyWorlds()
        {
            if (_rootSystem != null)
            {
                _rootSystem.TearDown();
                _rootSystem.DeactivateReactiveSystems();
                _rootSystem.ClearReactiveSystems();
                _rootSystem = null;
            }
            LogicWorld?.Reset();
            MetaWorld?.Reset();
            CreationInfo = null;
            LogicWorld = null;
            MetaWorld = null;
        }
        

        //////////////////////////////////////////////////////////////////////////
        /// 驱动:
        public virtual void Execute()
        {
            if (_rootSystem != null)
            {
                _rootSystem.Execute();
                _rootSystem.Cleanup();
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// CreationInfo
        public T GetCreationInfo<T>() where T : class
        {
            return CreationInfo as T;
        }
        
        
        //////////////////////////////////////////////////////////////////////////
        /// 内部初始化:
        protected virtual void RebuildComponentLookUp(Type lookupType, List<ComponentTypeIndex> typeIndexList, out List<Type> cmptTypes, out List<string> cmptNames)
        {
            typeIndexList.Clear();
            FieldInfo[] fieldInfos = lookupType.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            List<FieldInfo> cmptTypeIndexFields = new List<FieldInfo>();
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                //筛选出静态字段
                if (fieldInfo.FieldType == typeof(ComponentTypeIndex))
                {
                    cmptTypeIndexFields.Add(fieldInfo);
                }
            }

            //按字段名排序(确保顺序可预测)
            cmptTypeIndexFields.Sort((a, b) => a.Name.CompareTo(b.Name));

            cmptTypes = new List<Type>();
            cmptNames = new List<string>();

            //为每个ComponentTypeIndex实例的Index字段赋值，(CmptType字段静态初始化时就赋值了)
            for (int i = 0; i < cmptTypeIndexFields.Count; i++)
            {
                //获取字段的值(即ComponentTypeIndex实例)
                var staticFieldInst = cmptTypeIndexFields[i].GetValue(null) as ComponentTypeIndex;
                if (staticFieldInst != null)
                {
                    var cmptType = staticFieldInst.CmptType;
                    var cmptTypeName = cmptType.Name;
                    WLogger.Log($"重设组件Index，CmptIndex：{staticFieldInst.Index} -> {i}，cmptTypeName={cmptTypeName}");
                    staticFieldInst.Index = i;
                    typeIndexList.Add(staticFieldInst);
                    cmptTypes.Add(cmptType);
                    cmptNames.Add(cmptTypeName);
                }
                else
                {
                    WLogger.LogError("RebuildComponentLookUp staticFieldInst == null");
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// 扩展:
        protected virtual void CreateSystems()
        {
            if (CreationInfo?.CreateRootSystems == null)
                throw new InvalidOperationException("WorldCreationInfo.CreateRootSystems must be set before InitWorlds.");
            _rootSystem = CreationInfo.CreateRootSystems(CreationInfo, this);
        }


        //扩展可以使用LogicEntity子类
        protected virtual Func<LogicEntity> GetLogicEntityFactory()
        {
            return () => new LogicEntity();
        }

        //扩展可以使用LogicWorld子类
        protected virtual void CreateLogicWorld()
        {
            RebuildComponentLookUp(typeof(LogicComponentsLookup), LogicComponentsLookup.TypeIndexList, out var cmptTypes, out var cmptNames);
            var contextInfo = new ContextInfo("LogicWorld", cmptNames.ToArray(), cmptTypes.ToArray());
            LogicWorld = new LogicWorld(contextInfo, cmptTypes.Count, GetLogicEntityFactory(), 1, (entity) => new UnsafeAERC());
        }

        //扩展可以使用MetaEntity子类
        protected virtual Func<MetaEntity> GetMetaEntityFactory()
        {
            return () => new MetaEntity();
        }

        //扩展可以使用MetaWorld子类
        protected virtual void CreateMetaWorld()
        {
            RebuildComponentLookUp(typeof(MetaComponentsLookup), MetaComponentsLookup.TypeIndexList, out var cmptTypes, out var cmptNames);
            var contextInfo = new ContextInfo("MetaWorld", cmptNames.ToArray(), cmptTypes.ToArray());
            MetaWorld = new MetaWorld(contextInfo, cmptTypes.Count, GetMetaEntityFactory(), 12300001);
        }

        //////////////////////////////////////////////////////////////////////////
        public static bool operator !(ECWorlds world)
        {
            return world == null;
        }

        public static bool operator true(ECWorlds world)
        {
            return world != null;
        }

        public static bool operator false(ECWorlds world)
        {
            return world == null;
        }
    }
}