using System.Collections.Generic;
using Entitas;

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
            return entity.hasComView && entity.comView.HasPendingAcquire;
        }

        protected override void Execute(List<LogicEntity> entities)
        {
            foreach (var entity in entities)
            {
                AcquireViews(entity);
            }
        }

        private static void AcquireViews(LogicEntity entity)
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

        private static void BeginAcquire(LogicEntity entity, ViewComponent view, IViewAcquirable acquirable)
        {
            view.MarkLoading(acquirable);

            // 静态方法组绑定，避免每帧闭包分配
            var ctx = new ViewAcquireContext
            {
                Entity = entity,
                View = view,
                Acquirable = acquirable,
                OnCompleted = OnAcquireCompleted,
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
                view.MarkFailed(acquirable);
                return;
            }

            view.MarkReady(acquirable);
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
