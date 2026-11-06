using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary>
    /// 命中结算出口，由项目侧实现并挂到 UniSubobjectsComponent.HitSink。负责伤害、特效与战果记账；
    /// 框架只保证每条事件都带着出生时的 Source 与 Origin。未挂出口时命中仍照常计次。
    /// </summary>
    public interface ISubobjectHitSink
    {
        /// <summary>
        /// 主线程每帧最多调用一次，hits 为本帧全部命中，返回后会被清空。
        /// 回调内可以生成或回收子物体，也可以给目标挂死亡组件。
        /// </summary>
        void OnSubobjectHits(UniSubobjectsComponent subobjects, List<SubobjectHitEvent> hits);
    }

    /// <summary>
    /// 批处理记录的表现出口，由项目侧实现并挂到 UniSubobjectsComponent.ViewSink。
    /// 只处理 ViewKey 非 0 且未关联实体的记录；关联实体走自身的 ViewComponent。
    /// </summary>
    public interface ISubobjectViewSink
    {
        /// <summary>
        /// 记录生成后立刻调用，viewKey 为生成参数中的表现键。
        /// </summary>
        void OnSpawn(SubobjectHandle handle, int viewKey, in SubobjectMotion motion);

        /// <summary>
        /// 每帧对每个套路调用一次，在回收之前。实现方遍历 routine 的稠密记录，
        /// 跳过 ViewKeyAt 为 0 或 LinkedAt 非空的条目，按 HandleAt 找到自己的表现并同步 MotionAt。
        /// </summary>
        void SyncRoutine(SubobjectRoutine routine);

        /// <summary>
        /// 记录回收时调用，之后 handle 失效。
        /// </summary>
        void OnDespawn(SubobjectHandle handle);
    }
}
