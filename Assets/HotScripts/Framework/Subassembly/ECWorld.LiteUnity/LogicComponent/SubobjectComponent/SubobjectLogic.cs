using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary>
    /// 关心关联记录命中的节点实现此接口；只有处于激活状态的节点会收到通知。
    /// </summary>
    public interface ISubobjectHitNotify
    {
        /// <summary>
        /// 关联记录命中目标后、在主线程派发阶段调用。
        /// </summary>
        void OnRoutineHit(in SubobjectHitEvent hit);
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 实体子物体的自定义逻辑，由 SubobjectComponent 持有，SysSubobject 逐帧 Update。
    /// 用于无人机、子母弹、智能地雷等复杂子物体；GenInfo 为 SubobjectGenInfo，可取到来源、父级与玩家。
    /// 与关联记录共存时，记录的命中会经 OnRoutineHit 转给实现 ISubobjectHitNotify 的节点。
    /// </summary>
    public class SubobjectLogic : CustomLogic
    {
        protected List<ISubobjectHitNotify> _hitNotifyList = new();

        public SubobjectGenInfo SubobjectGenInfo
        {
            get { return GetGenInfo<SubobjectGenInfo>(); }
        }

        //////////////////////////////////////////////////////////////////////////
        /// CustomLogic：override

        protected override void ClearInterfaceCache()
        {
            _hitNotifyList.Clear();
            base.ClearInterfaceCache();
        }

        protected override void CacheInterface(CustomNode node)
        {
            base.CacheInterface(node);
            TraverseCollectInterface(ref _hitNotifyList, node);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>
        /// 把关联记录的一次命中转给激活中的 ISubobjectHitNotify 节点。
        /// </summary>
        public virtual void OnRoutineHit(in SubobjectHitEvent hit)
        {
            for (var i = 0; i < _hitNotifyList.Count; i++)
            {
                var notify = _hitNotifyList[i];
                if (notify is ICustomNode node && node.IsActive)
                    notify.OnRoutineHit(in hit);
            }
        }
    }
}
