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
    // IAssetViewLoadable：按 AssetLocation 异步加载资源
    public interface IAssetViewLoadable
    {
        ViewLoadState LoadState { get; }
        string AssetLocation { get; }
        void RequestLoad(string assetLocation);
        void SetLoadState(ViewLoadState state);
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
        // IAssetViewLoadable
        private readonly List<IAssetViewLoadable> _assetLoadables = new();
        public IReadOnlyList<IAssetViewLoadable> AssetLoadables => _assetLoadables;

        public bool HasPendingAssetLoad
        {
            get
            {
                for (int i = 0; i < _assetLoadables.Count; ++i)
                {
                    var loadable = _assetLoadables[i];
                    if (loadable.LoadState == ViewLoadState.None && !string.IsNullOrEmpty(loadable.AssetLocation))
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
            if (vw is IAssetViewLoadable loadable)
                _assetLoadables.Add(loadable);

            if (vw is IViewTransformSyncable syncable)
                _transformSyncables.Add(syncable);
        }

        protected virtual void ClearInterfaceCache()
        {
            _assetLoadables.Clear();
            _transformSyncables.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        // IAssetViewLoadable 状态推进
        public void MarkLoading(IAssetViewLoadable loadable)
        {
            SetLoadState(loadable, ViewLoadState.Loading);
        }

        public void MarkReady(IAssetViewLoadable loadable)
        {
            SetLoadState(loadable, ViewLoadState.Ready);
        }

        public void MarkFailed(IAssetViewLoadable loadable)
        {
            SetLoadState(loadable, ViewLoadState.Failed);
        }

        private void SetLoadState(IAssetViewLoadable loadable, ViewLoadState state)
        {
            if (loadable == null || loadable.LoadState == state)
                return;

            loadable.SetLoadState(state);
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

            var wrapper = viewWrapper ?? new ViewWrapperBase();
            if (wrapper is IAssetViewLoadable loadable)
                loadable.RequestLoad(assetLocation);

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
