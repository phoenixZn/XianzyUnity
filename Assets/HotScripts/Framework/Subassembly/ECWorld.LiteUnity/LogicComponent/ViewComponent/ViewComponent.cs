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
        void BindProxy(IViewTransformProxy proxy);
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

    public struct ViewAcquireContext
    {
        /// <summary>
        /// 成功时 proxy 已由策略 BindProxy；失败时 proxy 可为 null。
        /// </summary>
        public Action<bool, IViewTransformProxy> OnCompleted;
    }

    //////////////////////////////////////////////////////////////////////////
    // IViewTransformSyncable：需要同步逻辑 Transform 到表现
    public interface IViewTransformSyncable
    {
        bool SyncTransform { get; set; }
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
                    if (_transformSyncables[i].SyncTransform)
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
        // IViewAcquirable 状态推进
        public void MarkLoading(IViewAcquirable acquirable)
        {
            SetLoadState(acquirable, ViewLoadState.Loading);
        }

        public void MarkReady(IViewAcquirable acquirable)
        {
            SetLoadState(acquirable, ViewLoadState.Ready);
        }

        public void MarkFailed(IViewAcquirable acquirable)
        {
            SetLoadState(acquirable, ViewLoadState.Failed);
        }

        private void SetLoadState(IViewAcquirable acquirable, ViewLoadState state)
        {
            if (acquirable == null || acquirable.LoadState == state)
                return;

            acquirable.SetLoadState(state);
            NotifyChanged();
        }

        private void NotifyChanged()
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

        public void SetComView(string assetLocation, IViewWrapper viewWrapper = null)
        {
            var index = LogicComponentsLookup.ComView;
            if (!hasComView)
            {
                var component = (ViewComponent)CreateComponent(index, typeof(ViewComponent));
                component.Init();
                AddComponent(index, component);
            }

            var wrapper = viewWrapper ?? new AsyncAssetViewWrapper(assetLocation);
            comView.AddViewWrapper(wrapper);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class EntityExtension
    {
        public static void RequestViewLoad(this LogicEntity entity, string assetLocation)
        {
            if (entity == null)
                return;
            entity.SetComView(assetLocation);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComViewIndex = new(typeof(ViewComponent));
        public static int ComView => _ComViewIndex.Index;
    }
}
