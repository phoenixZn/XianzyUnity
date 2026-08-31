using System;
using Entitas;

namespace Xease.CoreGame
{
    public partial class LogicEntity : Entity
    {
        //////////////////////////////////////////////////////////////////////////
        /// Entity：override

        // PostInitialize：组件加入且 event 已派发
        protected override void OnComponentAddedEx(int index, IComponent component)
        {
            if (component is LogicComponent theCmpt)
            {
                SafePostInitialize(theCmpt);
            }
        }

        // DisposeOnRemove：组件移除且 event 已派发
        protected override void OnComponentRemovedEx(int index, IComponent component)
        {
            if (component is IComponentDispose dispose)
            {
                SafeDisposeOnRemove(dispose);
            }
        }

        // 先 Dispose 旧组件再 PostInitialize 新组件；同一引用则跳过
        protected override void OnComponentReplacedEx(int index, IComponent previousComponent, IComponent newComponent)
        {
            if (previousComponent == newComponent)
            {
                return;
            }
            if (previousComponent is IComponentDispose dispose)
            {
                SafeDisposeOnRemove(dispose);
            }
            if (newComponent is LogicComponent theCmpt)
            {
                SafePostInitialize(theCmpt);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        public LogicWorld OwnerWorld { get; private set; }

        /// <summary>
        /// 实体进入 LogicWorld，仅绑定 OwnerWorld。组件回调走 Entity 钩子，不对 OnComponent* event +=。
        /// </summary>
        public virtual void EnterWorld(LogicWorld logicWorld)
        {
            OwnerWorld = logicWorld;
        }

        public virtual void WillBeLeaveWorld()
        {
        }

        public virtual void LeaveWorld()
        {
        }

        protected void SafePostInitialize(LogicComponent theCmpt)
        {
            try
            {
                theCmpt.PostInitialize(this);
            }
            catch (Exception e)
            {
                WLogger.LogError($"LogicComponent PostInitialize catch Exception:{e}");
            }
        }

        protected void SafeDisposeOnRemove(IComponentDispose dispose)
        {
            try
            {
                dispose.DisposeOnRemove();
            }
            catch (Exception e)
            {
                WLogger.LogError($"LogicComponent DisposeOnRemove catch Exception:{e}");
            }
        }
    }
}
