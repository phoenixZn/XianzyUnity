using System;

namespace Xease
{
    /// <summary>
    /// 三级时间轮定时器服务：面向非关键帧事务（Buff Tick、CD、AI 轮询等），主线程驱动。
    /// </summary>
    public interface ITimerService : IService
    {
        /// <summary>
        /// 添加定时器。
        /// </summary>
        /// <param name="callback">回调，参数为当前执行次数（从 1 开始）</param>
        /// <param name="intervalSec">间隔秒数（内部转毫秒，按 10ms 刻度向下对齐，最小 10ms）</param>
        /// <param name="repeatCount">1=单次，-1=无限循环，N=指定次数</param>
        /// <param name="useUnscaled">true=无视 timeScale（UI/真实时间），false=跟随游戏缩放</param>
        /// <returns>定时器唯一 ID；失败返回 0</returns>
        int AddTimer(Action<int> callback, float intervalSec = 1f, int repeatCount = 1, bool useUnscaled = false);

        /// <summary>
        /// 移除定时器（惰性删除；允许在回调内移除自身）。
        /// </summary>
        bool RemoveTimer(int timerId);
    }
}
