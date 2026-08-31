using System;
using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public sealed class SysViewLoader : ReactiveSystem<LogicEntity>
    {
        // 实例级缓存，避免每次 BeginAcquire 分配 Action
        private readonly Action<ViewAcquireContext> _onAcquireCompleted;

        public SysViewLoader(ECWorlds worlds) : base(worlds.LogicWorld)
        {
            _onAcquireCompleted = OnAcquireCompleted;
        }

        protected override ICollector<LogicEntity> GetTrigger(IContext<LogicEntity> context)
        {
            return context.CreateCollector(LogicMatcher.AllOf(LogicComponentsLookup.ComView));
        }

        protected override bool Filter(LogicEntity entity)
        {
            return entity.hasComView && entity.comView.HasPendingAcquire;
        }

        protected override void Execute(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                AcquireViews(entity);
            }
        }

        private void AcquireViews(LogicEntity entity)
        {
            var view = entity.comView;
            var acquirables = view.Acquirables;
            for (int i = 0; i < acquirables.Count; ++i)
            {
                var acquirable = acquirables[i];
                if (!acquirable.HasPendingAcquire)
                    continue;

                BeginAcquire(entity, view, acquirable);
            }
        }

        private void BeginAcquire(LogicEntity entity, ViewComponent view, IViewAcquirable acquirable)
        {
            SetLoadState(view, acquirable, ViewLoadState.Loading);

            var ctx = new ViewAcquireContext
            {
                Entity = entity,
                View = view,
                Acquirable = acquirable,
                OnCompleted = _onAcquireCompleted,
            };
            acquirable.BeginAcquire(ctx);
        }

        private static void OnAcquireCompleted(ViewAcquireContext ctx)
        {
            var entity = ctx.Entity;
            var view = ctx.View;
            var acquirable = ctx.Acquirable;

            if (entity == null || !entity.isEnabled || !entity.hasComView || entity.comView != view)
                return;

            if (!ctx.Success)
            {
                SetLoadState(view, acquirable, ViewLoadState.Failed);
                return;
            }

            SetLoadState(view, acquirable, ViewLoadState.Ready);
            SyncTransformFromEntity(entity);
        }

        // 推进 acquirable 加载状态并 dirty ViewComponent
        private static void SetLoadState(ViewComponent view, IViewAcquirable acquirable, ViewLoadState state)
        {
            if (view == null || acquirable == null || acquirable.LoadState == state)
                return;

            acquirable.SetLoadState(state);
            view.NotifyChanged();
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
                if (!syncable.NeedsSyncTransform)
                    continue;

                syncable.ApplyTransform(transform.position, transform.rotation, transform.scale);
            }
        }
    }
}
