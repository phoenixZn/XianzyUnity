using System;
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

    public interface IViewWrapper : IDisposable
    {
        bool IsReady { get; }
        ViewLoadState LoadState { get; }
        void RequestLoad(string assetLocation);
        void BindProxy(IViewTransformProxy proxy);
        void ApplyTransform(Vector3 position, Quaternion rotation, Vector3 scale);
        void SetActive(bool active);
    }

    public class ViewComponent : LogicComponent, IComponentDispose
    {
        public string assetLocation { get; private set; }
        public ViewLoadState loadState { get; private set; } = ViewLoadState.None;
        public bool syncTransform { get; set; } = true;
        public IViewWrapper wrapper { get; private set; }

        public void Init(string location, IViewWrapper viewWrapper = null)
        {
            assetLocation = location;
            loadState = ViewLoadState.None;
            syncTransform = true;
            wrapper = viewWrapper ?? new ViewWrapperBase();
            wrapper.RequestLoad(location);
        }

        public void RequestLoad(string location)
        {
            assetLocation = location;
            loadState = ViewLoadState.None;
            wrapper ??= new ViewWrapperBase();
            wrapper.RequestLoad(location);
            NotifyChanged();
        }

        public void AttachWrapper(IViewWrapper viewWrapper)
        {
            if (viewWrapper == null)
                return;

            wrapper?.Dispose();
            wrapper = viewWrapper;
            if (!string.IsNullOrEmpty(assetLocation))
                wrapper.RequestLoad(assetLocation);
            NotifyChanged();
        }

        public void MarkLoading()
        {
            SetLoadState(ViewLoadState.Loading);
        }

        public void MarkReady()
        {
            SetLoadState(ViewLoadState.Ready);
            if (wrapper is ViewWrapperBase viewWrapperBase)
                viewWrapperBase.SetLoadState(ViewLoadState.Ready);
        }

        public void MarkFailed()
        {
            SetLoadState(ViewLoadState.Failed);
            if (wrapper is ViewWrapperBase viewWrapperBase)
                viewWrapperBase.SetLoadState(ViewLoadState.Failed);
        }

        private void SetLoadState(ViewLoadState state)
        {
            if (loadState == state)
                return;

            loadState = state;
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
            wrapper?.Dispose();
            wrapper = null;
            assetLocation = null;
            loadState = ViewLoadState.None;
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
                component.Init(assetLocation, viewWrapper);
                AddComponent(index, component);
                return;
            }

            if (viewWrapper != null)
                comView.AttachWrapper(viewWrapper);
            comView.RequestLoad(assetLocation);
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
