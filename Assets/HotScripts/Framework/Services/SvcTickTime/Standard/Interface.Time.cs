using System;

namespace Xease
{

    //////////////////////////////////////////////////////////////////////////
    
    // 值类型事件系统接口
    public interface ITickTimeService : IService
    {
        public float fixedDeltaTime { get; }
        public float fixedUnscaledDeltaTime { get; }
        public float deltaTime { get; }
        public float unscaledDeltaTime { get; }
        public float timeScale { get; }

        /// <summary>当前游戏基准倍速（只读，通过 SetGameSpeed 修改）。</summary>
        public float GameSpeed { get; }

        /// <summary>当前全局减速系数（所有已登记减速中的最小值，通过 SetTimeSlowRatio/ClearTimeSlowRatio 修改）。</summary>
        public float TimeSlowRatio { get; }

        /// <summary>设置游戏基准倍速，向上调节用此接口（>1，如全局倍速）。</summary>
        public void SetGameSpeed(float timeSpeed);

        /// <summary>登记一路减速（<1，如暂停/子弹时间）；相同 slowFlag 重复登记会覆盖。</summary>
        public void SetTimeSlowRatio(int slowFlag, float scale);

        /// <summary>按 slowFlag 移除一路减速，全部移除后回到基准倍速。</summary>
        public void ClearTimeSlowRatio(int slowFlag);

    }

}