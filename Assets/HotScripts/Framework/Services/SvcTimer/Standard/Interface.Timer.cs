using System;

namespace Xease
{
    /// <summary>
    /// 三级时间轮定时器服务：面向非关键帧事务（Buff Tick、CD、AI 轮询等），主线程驱动。
    /// 默认同一 EnvUpdate 内每个节点最多回调一次；错过的 tick 不补齐，ExecutedCount 按实际 Fire 增加。
    /// </summary>
    public interface ITimerService : IService
    {
        /// <summary>
        /// 添加定时器。默认每帧每节点最多回调一次（实现侧 MaxOneFirePerUpdate）；卡顿不补齐次数。
        /// </summary>
        /// <param name="callback">回调，参数为当前执行次数（从 1 开始）</param>
        /// <param name="intervalSec">间隔秒数（内部转毫秒，按 10ms 刻度向下对齐，最小 10ms）</param>
        /// <param name="repeatCount">1=单次，-1=无限循环，N=指定次数</param>
        /// <param name="useTimeScale">false=无视 timeScale（UI/真实时间），true=跟随游戏缩放</param>
        /// <returns>定时器唯一 ID；失败返回 0</returns>
        int AddTimer(Action<int> callback, float intervalSec = 1f, int repeatCount = 1, bool useTimeScale = false);

        /// <summary>
        /// 移除定时器（惰性删除；允许在回调内移除自身）。
        /// </summary>
        bool RemoveTimer(int timerId);
    }
}
