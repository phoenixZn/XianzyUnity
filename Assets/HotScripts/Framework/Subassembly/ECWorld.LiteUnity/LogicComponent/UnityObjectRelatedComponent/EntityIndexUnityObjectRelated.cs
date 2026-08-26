using System.Collections.Generic;
using Entitas;
using UnityEngine;

namespace Xease.CoreGame
{
    //////////////////////////////////////////////////////////////////////////
    /// This：
    public partial class LogicWorld
    {
        /// <summary>
        /// UnityObjectRelated 反向索引缓存；由 AddEntityIndex_UnityObjectRelated 写入。
        /// </summary>
        public UnityObjectRelatedEntityIndex IndexUnityObjectRelated { get; internal set; }
    }

    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComUnityObjectRelated
    public static partial class WorldExtension
    {
        /// <summary>
        /// 注册 UnityObjectRelated 反向索引；应在世界初始化时调用一次。
        /// </summary>
        public static void AddEntityIndex_UnityObjectRelated(this LogicWorld world)
        {
            var index = new UnityObjectRelatedEntityIndex(
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComUnityObjectRelated)));
            world.AddEntityIndex(index);
            world.IndexUnityObjectRelated = index;
        }

        /// <summary>
        /// 按 GameObject InstanceID 查唯一关联 entity；未登记或未建索引时返回 null。
        /// </summary>
        public static LogicEntity GetEntityWithUnityObjectRelated(this LogicWorld world, int gameObjectInstanceID)
        {
            if (world == null || world.IndexUnityObjectRelated == null)
                return null;
            return world.IndexUnityObjectRelated.GetEntity(gameObjectInstanceID);
        }

        /// <summary>
        /// 按 GameObject 查唯一关联 entity；go 为 null（含已 Destroy）时返回 null。
        /// </summary>
        public static LogicEntity GetEntityWithUnityObjectRelated(this LogicWorld world, GameObject go)
        {
            if (go == null)
                return null;
            return world.GetEntityWithUnityObjectRelated(go.GetInstanceID());
        }

        /// <summary>
        /// 按 InstanceID 尝试查询；成功时写出 entity。
        /// </summary>
        public static bool TryGetEntityWithUnityObjectRelated(this LogicWorld world, int gameObjectInstanceID, out LogicEntity entity)
        {
            entity = null;
            if (world == null || world.IndexUnityObjectRelated == null)
                return false;
            return world.IndexUnityObjectRelated.TryGetEntity(gameObjectInstanceID, out entity);
        }

        /// <summary>
        /// 按 GameObject 尝试查询；go 为 null 时失败。
        /// </summary>
        public static bool TryGetEntityWithUnityObjectRelated(this LogicWorld world, GameObject go, out LogicEntity entity)
        {
            entity = null;
            if (go == null)
                return false;
            return world.TryGetEntityWithUnityObjectRelated(go.GetInstanceID(), out entity);
        }
    }

    /// <summary>
    /// GameObject InstanceID → 唯一 LogicEntity 的反向主键索引。
    /// 增量 Bind/Unbind 走 AddKey/RemoveKey；组件 Add/Remove 时由 Group 事件按 RelationDic 全量登记。不经 getKeys，避免 ToArray。
    /// </summary>
    public sealed class UnityObjectRelatedEntityIndex : IEntityIndex
    {
        // 与 Context.AddEntityIndex 注册名一致
        public const string IndexName = "EntityIndex_UnityObjectRelated";

        // 订阅来源；Activate/Deactivate 成对增删事件
        readonly IGroup<LogicEntity> _group;
        // instanceID -> entity；1 个 GO 只能对应 1 个 entity
        readonly Dictionary<int, LogicEntity> _goToEntity;
        // 缓存委托，保证 -= 与 += 为同一实例
        readonly GroupChanged<LogicEntity> _onAdded;
        readonly GroupChanged<LogicEntity> _onRemoved;

        /// <summary>
        /// 绑定 Group 并立即 Activate。
        /// </summary>
        public UnityObjectRelatedEntityIndex(IGroup<LogicEntity> group)
        {
            _group = group;
            _goToEntity = new Dictionary<int, LogicEntity>();
            _onAdded = OnEntityAdded;
            _onRemoved = OnEntityRemoved;
            Activate();
        }

        ~UnityObjectRelatedEntityIndex() => Deactivate();

        //////////////////////////////////////////////////////////////////////////
        /// IEntityIndex:

        /// <summary>
        /// 与 Context.AddEntityIndex 使用的注册名一致。
        /// </summary>
        public string name => IndexName;

        /// <summary>
        /// 订阅 Group 并登记已有 entity；可重复调用（先摘事件再挂）。
        /// </summary>
        public void Activate()
        {
            _group.OnEntityAdded -= _onAdded;
            _group.OnEntityRemoved -= _onRemoved;
            _group.OnEntityAdded += _onAdded;
            _group.OnEntityRemoved += _onRemoved;
            IndexExisting();
        }

        /// <summary>
        /// 摘订阅并清空反向表，按 entity Release 一次。
        /// </summary>
        public void Deactivate()
        {
            _group.OnEntityAdded -= _onAdded;
            _group.OnEntityRemoved -= _onRemoved;
            Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>
        /// 增量登记一个 InstanceID。已被其他 entity 占用时返回 false；已是本 entity 则视为成功。
        /// 不 Retain；Retain 只在 Group OnEntityAdded 做一次。
        /// </summary>
        public bool AddKey(int instanceId, LogicEntity entity)
        {
            if (instanceId == 0 || entity == null)
                return false;

            if (_goToEntity.TryGetValue(instanceId, out var existing))
                return ReferenceEquals(existing, entity);

            _goToEntity.Add(instanceId, entity);
            return true;
        }

        /// <summary>
        /// 增量移除 InstanceID；仅当当前映射为本 entity 时删除。不 Release。
        /// </summary>
        public void RemoveKey(int instanceId, LogicEntity entity)
        {
            if (!_goToEntity.TryGetValue(instanceId, out var existing))
                return;
            if (!ReferenceEquals(existing, entity))
                return;
            _goToEntity.Remove(instanceId);
        }

        /// <summary>
        /// 按 InstanceID 取 entity；未登记返回 null。
        /// </summary>
        public LogicEntity GetEntity(int instanceId)
        {
            _goToEntity.TryGetValue(instanceId, out var entity);
            return entity;
        }

        /// <summary>
        /// 按 InstanceID 尝试取 entity。
        /// </summary>
        public bool TryGetEntity(int instanceId, out LogicEntity entity)
        {
            return _goToEntity.TryGetValue(instanceId, out entity);
        }

        void IndexExisting()
        {
            foreach (var entity in _group)
                OnEntityAdded(_group, entity, 0, null);
        }

        void OnEntityAdded(IGroup<LogicEntity> group, LogicEntity entity, int index, IComponent component)
        {
            var related = component as UnityObjectRelatedComponent ?? entity.comUnityObjectRelated;
            if (related == null || related.RelationDic == null)
                return;

            foreach (var kv in related.RelationDic)
            {
                if (!AddKey(kv.Key, entity))
                    WLogger.LogError($"UnityObjectRelatedEntityIndex 冲突 instanceId={kv.Key}");
            }

            RetainEntity(entity);
        }

        void OnEntityRemoved(IGroup<LogicEntity> group, LogicEntity entity, int index, IComponent component)
        {
            // previous 组件在 DisposeOnRemove.Clear 之前仍持完整 dic
            var related = component as UnityObjectRelatedComponent;
            if (related != null && related.RelationDic != null)
            {
                foreach (var kv in related.RelationDic)
                    RemoveKey(kv.Key, entity);
            }

            ReleaseEntity(entity);
        }

        void Clear()
        {
            foreach (var entity in _goToEntity.Values)
                ReleaseEntity(entity);
            _goToEntity.Clear();
        }

        void RetainEntity(LogicEntity entity)
        {
            if (entity.aerc is SafeAERC safeAerc)
            {
                if (!safeAerc.owners.Contains(this))
                    entity.Retain(this);
                return;
            }

            entity.Retain(this);
        }

        void ReleaseEntity(LogicEntity entity)
        {
            if (entity.aerc is SafeAERC safeAerc)
            {
                if (safeAerc.owners.Contains(this))
                    entity.Release(this);
                return;
            }

            entity.Release(this);
        }
    }
}
