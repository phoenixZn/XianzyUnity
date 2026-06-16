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
            if (!entity.hasComView)
                return false;
            var view = entity.comView;
            if (view.loadState != ViewLoadState.None)
                return false;
            return !string.IsNullOrEmpty(view.assetLocation);
        }

        protected override void Execute(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                LoadView(entity);
            }
        }

        private static void LoadView(LogicEntity entity)
        {
            var view = entity.comView;
            view.MarkLoading();

            var assetSvc = GEnv.Inst?.Services?.AssetSvc;
            if (assetSvc == null)
            {
                BindNullProxy(entity, view);
                return;
            }

            var location = view.assetLocation;
            assetSvc.LoadAssetAsync<GameObject>(location, handle => OnAssetLoaded(entity, view, location, handle));
        }

        private static void OnAssetLoaded(LogicEntity entity, ViewComponent view, string location, AssetHandle handle)
        {
            if (entity == null || !entity.isEnabled || !entity.hasComView || entity.comView != view)
                return;

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                WLogger.LogError($"SysViewLoader load failed: {location}");
                view.MarkFailed();
                return;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                WLogger.LogError($"SysViewLoader asset is not GameObject: {location}");
                view.MarkFailed();
                return;
            }

            var instance = Object.Instantiate(prefab);
            var proxy = new UnityViewTransformProxy(instance.transform);
            view.wrapper.BindProxy(proxy);
            view.MarkReady();
            SyncTransformFromEntity(entity);
        }

        private static void BindNullProxy(LogicEntity entity, ViewComponent view)
        {
            view.wrapper.BindProxy(NullViewTransformProxy.Instance);
            view.MarkReady();
            SyncTransformFromEntity(entity);
        }

        internal static void SyncTransformFromEntity(LogicEntity entity)
        {
            if (!entity.hasComTransform || !entity.hasComView)
                return;

            var view = entity.comView;
            if (!view.syncTransform || view.wrapper == null)
                return;

            var transform = entity.comTransform;
            view.wrapper.ApplyTransform(transform.position, transform.rotation, transform.scale);
        }
    }
}
