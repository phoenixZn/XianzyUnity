using System.Collections.Generic;
using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// GameObject 关联类型常量；使用方以 partial 扩展（如 HitCollider / View / UI）。
    /// </summary>
    public static partial class UnityObjectRelation
    {
        // 未指定关联类型
        public const int Unknown = 0;
    }

    /// <summary>
    /// 记录 entity 关联的 GameObject InstanceID（一对多正向表）。
    /// 字典由组件自持，禁止换成外部引用；增删改只经 LogicEntity Bind/Unbind/Rebind。
    /// GO Destroy/还池前须 Unbind，避免 InstanceID 复用指到错误 entity。
    /// </summary>
    public class UnityObjectRelatedComponent : LogicComponent
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // instanceID -> UnityObjectRelation；自持，禁止换成外部引用，Clear 保留容量
        public Dictionary<int, int> RelationDic { get; private set; } = new Dictionary<int, int>(4);

        /// <summary>
        /// 取 instanceID 的关联类型；未登记返回 <see cref="UnityObjectRelation.Unknown"/>。
        /// </summary>
        public int GetUnityObjectRelation(int instanceID)
        {
            if (TryGetUnityObjectRelation(instanceID, out var relation))
                return relation;
            return UnityObjectRelation.Unknown;
        }

        /// <summary>
        /// 尝试取 instanceID 的关联类型；未登记返回 false。
        /// </summary>
        public bool TryGetUnityObjectRelation(int instanceID, out int relation)
        {
            if (RelationDic == null)
            {
                relation = UnityObjectRelation.Unknown;
                return false;
            }

            return RelationDic.TryGetValue(instanceID, out relation);
        }

        // 池化复用时保证 dic 非 null
        internal Dictionary<int, int> EnsureRelationDic()
        {
            RelationDic ??= new Dictionary<int, int>(4);
            return RelationDic;
        }

        /// <summary>
        /// 原地改 dic 后通知 Group/Collector；同实例 Replace，不进组件池、不 Dispose。
        /// Add/Remove 组件本身已会派发，无需再调。
        /// </summary>
        public void NotifyChanged()
        {
            if (_hostEntity == null)
                return;
            _hostEntity.ReplaceComponent(LogicComponentsLookup.ComUnityObjectRelated, this);
        }

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        public override void DisposeOnRemove()
        {
            base.DisposeOnRemove();
            EnsureRelationDic().Clear();
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

        /// <summary>
        /// 登记一个 GameObject InstanceID。同一 entity 重复 Bind 只更新 relation。
        /// 首次 AddComponent；已有组件则原地改 dic 后 NotifyChanged。
        /// </summary>
        public bool BindUnityObject(int instanceId, int relation = UnityObjectRelation.Unknown)
        {
            if (instanceId == 0)
            {
                WLogger.LogError("BindUnityObject instanceId == 0");
                return false;
            }

            var cmptIndex = LogicComponentsLookup.ComUnityObjectRelated;
            if (!hasComUnityObjectRelated)
            {
                var component = (UnityObjectRelatedComponent)CreateComponent(cmptIndex, typeof(UnityObjectRelatedComponent));
                component.EnsureRelationDic()[instanceId] = relation;
                AddComponent(cmptIndex, component);
                return true;
            }

            var related = comUnityObjectRelated;
            var dic = related.EnsureRelationDic();
            if (dic.TryGetValue(instanceId, out var oldRelation))
            {
                if (oldRelation == relation)
                    return true;
                dic[instanceId] = relation;
                related.NotifyChanged();
                return true;
            }

            var relatedIndex = OwnerWorld?.IndexUnityObjectRelated;
            if (relatedIndex != null && !relatedIndex.AddKey(instanceId, this))
                return false;

            dic[instanceId] = relation;
            related.NotifyChanged();
            return true;
        }

        /// <summary>
        /// 按 GameObject 登记；go 为 null（含已 Destroy）时失败。
        /// </summary>
        public bool BindUnityObject(GameObject go, int relation = UnityObjectRelation.Unknown)
        {
            if (go == null)
            {
                WLogger.LogError("BindUnityObject go == null");
                return false;
            }

            return BindUnityObject(go.GetInstanceID(), relation);
        }

        /// <summary>
        /// 解除一个 InstanceID。dic 清空时 Remove 组件；否则 NotifyChanged。
        /// </summary>
        public bool UnbindUnityObject(int instanceId)
        {
            if (!hasComUnityObjectRelated)
                return false;

            var related = comUnityObjectRelated;
            var dic = related.EnsureRelationDic();
            if (!dic.Remove(instanceId))
                return false;

            OwnerWorld?.IndexUnityObjectRelated?.RemoveKey(instanceId, this);

            if (dic.Count == 0)
                RemoveComUnityObjectRelated();
            else
                related.NotifyChanged();

            return true;
        }

        /// <summary>
        /// 按 GameObject 解除登记；go 为 null 时失败。
        /// </summary>
        public bool UnbindUnityObject(GameObject go)
        {
            if (go == null)
                return false;
            return UnbindUnityObject(go.GetInstanceID());
        }

        /// <summary>
        /// 换绑 InstanceID（变身/换模型/上下载具）。oldId 与 newId 相同则只更新 relation。
        /// </summary>
        public bool RebindUnityObject(int oldId, int newId, int relation = UnityObjectRelation.Unknown)
        {
            if (oldId == newId)
                return BindUnityObject(newId, relation);

            if (oldId != 0)
                UnbindUnityObject(oldId);

            return BindUnityObject(newId, relation);
        }

        /// <summary>
        /// 按 GameObject 换绑；newGo 为 null 时失败。
        /// </summary>
        public bool RebindUnityObject(GameObject oldGo, GameObject newGo, int relation = UnityObjectRelation.Unknown)
        {
            if (newGo == null)
            {
                WLogger.LogError("RebindUnityObject newGo == null");
                return false;
            }

            var oldId = oldGo != null ? oldGo.GetInstanceID() : 0;
            return RebindUnityObject(oldId, newGo.GetInstanceID(), relation);
        }

        /// <summary>
        /// 解除本 entity 全部 GameObject 关联并移除组件。
        /// </summary>
        public void ClearUnityObjectRelated()
        {
            if (!hasComUnityObjectRelated)
                return;
            RemoveComUnityObjectRelated();
        }

        public void RemoveComUnityObjectRelated()
        {
            RemoveComponent(LogicComponentsLookup.ComUnityObjectRelated);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComUnityObjectRelatedIndex = new ComponentTypeIndex(typeof(UnityObjectRelatedComponent));
        public static int ComUnityObjectRelated => _ComUnityObjectRelatedIndex.Index;
    }
}
