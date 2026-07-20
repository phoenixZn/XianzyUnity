using System.Collections.Generic;
using Entitas;
using UnityEngine;
using Xease;
using YooAsset;

namespace Xease.CoreGame
{
    public sealed class SysViewLoader : ReactiveSystem<LogicEntity>
    {
        public SysViewLoader(ECWorlds worlds) : base(worlds.LogicWorld)
        {
        }

        protected override ICollector<LogicEntity> GetTrigger(IContext<LogicEntity> context)
        {
            return context.CreateCollector(LogicMatcher.AllOf(LogicComponentsLookup.ComView));
        }

        protected override bool Filter(LogicEntity entity)
        {
            return entity.hasComView && entity.comView.HasPendingAssetLoad;
        }

        protected override void Execute(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                LoadViews(entity);
            }
        }

        private static void LoadViews(LogicEntity entity)
        {
            var view = entity.comView;
            var loadables = view.AssetLoadables;
            for (int i = 0; i < loadables.Count; ++i)
            {
                var loadable = loadables[i];
                if (loadable.LoadState != ViewLoadState.None || string.IsNullOrEmpty(loadable.AssetLocation))
                    continue;

                LoadView(entity, view, loadable);
            }
        }

        private static void LoadView(LogicEntity entity, ViewComponent view, IAssetViewLoadable loadable)
        {
            view.MarkLoading(loadable);

            var wrapper = loadable as IViewWrapper;
            if (wrapper == null)
            {
                WLogger.LogError($"SysViewLoader loadable is not IViewWrapper: {loadable.AssetLocation}");
                view.MarkFailed(loadable);
                return;
            }

            var assetSvc = GEnv.Inst?.Services?.AssetSvc;
            if (assetSvc == null)
            {
                BindNullProxy(entity, view, loadable, wrapper);
                return;
            }

            var location = loadable.AssetLocation;
            assetSvc.LoadAssetAsync<GameObject>(location, handle => OnAssetLoaded(entity, view, loadable, wrapper, location, handle));
        }

        private static void OnAssetLoaded(
            LogicEntity entity,
            ViewComponent view,
            IAssetViewLoadable loadable,
            IViewWrapper wrapper,
            string location,
            AssetHandle handle)
        {
            if (entity == null || !entity.isEnabled || !entity.hasComView || entity.comView != view)
                return;

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                WLogger.LogError($"SysViewLoader load failed: {location}");
                view.MarkFailed(loadable);
                return;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                WLogger.LogError($"SysViewLoader asset is not GameObject: {location}");
                view.MarkFailed(loadable);
                return;
            }

            var instance = Object.Instantiate(prefab);
            var proxy = new UnityViewTransformProxy(instance.transform);
            wrapper.BindProxy(proxy);
            view.MarkReady(loadable);
            SyncTransformFromEntity(entity);
        }

        private static void BindNullProxy(
            LogicEntity entity,
            ViewComponent view,
            IAssetViewLoadable loadable,
            IViewWrapper wrapper)
        {
            wrapper.BindProxy(NullViewTransformProxy.Instance);
            view.MarkReady(loadable);
            SyncTransformFromEntity(entity);
        }

        internal static void SyncTransformFromEntity(LogicEntity entity)
        {
            if (!entity.hasComTransform || !entity.hasComView)
                return;

            var view = entity.comView;
            if (!view.HasSyncTransform)
                return;

            var transform = entity.comTransform;
            var syncables = view.TransformSyncables;
            for (int i = 0; i < syncables.Count; ++i)
            {
                var syncable = syncables[i];
                if (!syncable.SyncTransform)
                    continue;

                syncable.ApplyTransform(transform.position, transform.rotation, transform.scale);
            }
        }
    }
}
