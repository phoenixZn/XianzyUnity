using System;
using System.Collections.Generic;
using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    public enum ViewLoadState
    {
        None,
        Loading,
        Ready,
        Failed,
    }

    //////////////////////////////////////////////////////////////////////////
    // IViewWrapper：基础表现能力
    public interface IViewWrapper : IDisposable
    {
        bool IsReady { get; }
        void SetActive(bool active);
    }

    //////////////////////////////////////////////////////////////////////////
    // IViewAcquirable：可获取表现对象（加载策略入口）
    public interface IViewAcquirable
    {
        ViewLoadState LoadState { get; }
        bool HasPendingAcquire { get; }
        void BeginAcquire(ViewAcquireContext ctx);
        void SetLoadState(ViewLoadState state);
    }

    /// <summary>
    /// 可配置资源定位地址的 View 包装；供 RequestViewLoad 约束与失败日志读取，不进入 IViewAcquirable。
    /// </summary>
    public interface IViewAssetLocatable
    {
        /// <summary>
        /// 当前资源定位地址；未配置时为 null 或空。
        /// </summary>
        string AssetLocation { get; }

        /// <summary>
        /// 写入待获取的资源定位地址；换地址时应丢弃进行中的加载。
        /// </summary>
        void SetAssetLocation(string assetLocation);
    }

    /// <summary>
    /// 持有可绑定的 Unity GameObject；供 BindUnityObject 等消费，不进入加载策略。
    /// </summary>
    public interface IViewGameObjectHolder
    {
        /// <summary>
        /// 已就绪的 GameObject；未加载或已释放为 null。
        /// </summary>
        GameObject Instance { get; }
    }

    /// <summary>
    /// Acquire 请求/结果袋：请求字段由调用方填写；结果字段由策略在回调前写入。
    /// </summary>
    public struct ViewAcquireContext
    {
        // 请求侧（SysViewLoader 填入）
        public LogicEntity Entity;
        public ViewComponent View;
        public IViewAcquirable Acquirable;

        // 结果侧仅 Success；Proxy 由策略在回调前 BindProxy
        public bool Success;

        public Action<ViewAcquireContext> OnCompleted;

        // 写入结果并回调；须在值拷贝上调用（如 var ctx = _pendingCtx; ctx.Complete(...)）
        public void Complete(bool success)
        {
            Success = success;
            OnCompleted?.Invoke(this);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    // IViewTransformSyncable：需要同步逻辑 Transform 到表现
    public interface IViewTransformSyncable
    {
        bool NeedsSyncTransform { get; set; }
        void ApplyTransform(Vector3 position, Quaternion rotation, Vector3 scale);
    }

    public class ViewComponent : LogicComponent, IComponentDispose
    {
        //////////////////////////////////////////////////////////////////////////
        // IViewWrapper
        private readonly List<IViewWrapper> _wrappers = new();
        public IReadOnlyList<IViewWrapper> Wrappers => _wrappers;

        //////////////////////////////////////////////////////////////////////////
        // IViewAcquirable
        private readonly List<IViewAcquirable> _acquirables = new();
        public IReadOnlyList<IViewAcquirable> Acquirables => _acquirables;

        public bool HasPendingAcquire
        {
            get
            {
                for (int i = 0; i < _acquirables.Count; ++i)
                {
                    if (_acquirables[i].HasPendingAcquire)
                        return true;
                }

                return false;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        // IViewTransformSyncable
        private readonly List<IViewTransformSyncable> _transformSyncables = new();
        public IReadOnlyList<IViewTransformSyncable> TransformSyncables => _transformSyncables;

        public bool HasSyncTransform
        {
            get
            {
                for (int i = 0; i < _transformSyncables.Count; ++i)
                {
                    if (_transformSyncables[i].NeedsSyncTransform)
                        return true;
                }

                return false;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        // Wrapper 管理
        public void Init(IViewWrapper viewWrapper = null)
        {
            if (viewWrapper != null)
                AddViewWrapper(viewWrapper);
        }

        public void AddViewWrapper(IViewWrapper viewWrapper)
        {
            if (viewWrapper == null)
                return;

            _wrappers.Add(viewWrapper);
            CacheInterface(viewWrapper);
            NotifyChanged();
        }

        protected virtual void CacheInterface(IViewWrapper vw)
        {
            if (vw is IViewAcquirable acquirable)
                _acquirables.Add(acquirable);

            if (vw is IViewTransformSyncable syncable)
                _transformSyncables.Add(syncable);
        }

        protected virtual void ClearInterfaceCache()
        {
            _acquirables.Clear();
            _transformSyncables.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        // Entitas dirty
        public void NotifyChanged()
        {
            if (_hostEntity == null)
                return;
            _hostEntity.ReplaceComponent(LogicComponentsLookup.ComView, this);
        }

        public override void DisposeOnRemove()
        {
            for (int i = 0; i < _wrappers.Count; ++i)
                _wrappers[i]?.Dispose();

            _wrappers.Clear();
            ClearInterfaceCache();
            _hostEntity = null;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public ViewComponent comView
        {
            get { return (ViewComponent)GetComponent(LogicComponentsLookup.ComView); }
        }

        public bool hasComView
        {
            get { return HasComponent(LogicComponentsLookup.ComView); }
        }

        public void SetComView(IViewWrapper viewWrapper)
        {
            var index = LogicComponentsLookup.ComView;
            if (!hasComView)
            {
                var component = (ViewComponent)CreateComponent(index, typeof(ViewComponent));
                component.Init();
                AddComponent(index, component);
            }
            comView.AddViewWrapper(viewWrapper);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class EntityExtension
    {
        /// <summary>
        /// 从 SharedPool 租用指定 View 包装并请求加载 assetLocation。
        /// </summary>
        public static void RequestViewLoad<T>(this LogicEntity entity, string assetLocation)
            where T : ViewWrapperBase, IViewAcquirable, IViewAssetLocatable, new()
        {
            if (entity == null)
                return;
            var wrapper = G.SharedPool.Rent<T>();
            wrapper.Reset();
            wrapper.SetAssetLocation(assetLocation);
            entity.SetComView(wrapper);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComViewIndex = new(typeof(ViewComponent));
        public static int ComView => _ComViewIndex.Index;
    }
}
