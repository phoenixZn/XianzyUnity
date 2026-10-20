namespace Xease
{
    /// <summary>
    /// TickTime 服务公共基类：合成时间缩放（基准倍速 × 全局减速系数），
    /// 并在缩放变化时通过 <see cref="ITickTimeService.timeScale"/> 同步到平台实现。
    /// </summary>
    public abstract class TimeServiceBase : ITickTimeService
    {
        // 全局减速系数表：取所有已登记 change 的最小值，初值 1 表示不减速
        protected MultChangeFloat_MIN _timeSlowRatio = new MultChangeFloat_MIN(1);

        // 游戏基准倍速，向上调节（>1 倍速）
        protected float _gameSpeed = 1f;

        /// <summary>最终时间缩放：基准倍速与全局减速系数的乘积。</summary>
        public float FinalTimeScale => _gameSpeed * _timeSlowRatio.Value;

        /// <summary>当前游戏基准倍速（只读，通过 SetGameSpeed 修改）。</summary>
        public float GameSpeed => _gameSpeed;

        /// <summary>当前全局减速系数（所有已登记减速中的最小值，初值 1 表示未减速，通过 SetTimeSlowRatio/ClearTimeSlowRatio 修改）。</summary>
        public float TimeSlowRatio => _timeSlowRatio.Value;


        //////////////////////////////////////////////////////////////////////////
        /// ITickTimeService:
        
        public abstract float fixedDeltaTime { get; }
        public abstract float fixedUnscaledDeltaTime { get; }
        public abstract float deltaTime { get; }
        public abstract float unscaledDeltaTime { get; }
        public abstract float timeScale { get; protected set; }

        public abstract void Shutdown();

        /// <summary>设置游戏基准倍速，向上调节用此接口（>1，如全局倍速）。</summary>
        public void SetGameSpeed(float timeSpeed)
        {
            _gameSpeed = timeSpeed;
            SyncTimeScale();
        }

        /// <summary>登记一路减速（<1，如暂停/子弹时间）；相同 slowFlag 重复登记会覆盖。</summary>
        public void SetTimeSlowRatio(int slowFlag, float scale)
        {
            _timeSlowRatio.AddChange(scale, slowFlag);
            SyncTimeScale();
        }

        /// <summary>按 slowFlag 移除一路减速，全部移除后回到基准倍速。</summary>
        public void ClearTimeSlowRatio(int slowFlag)
        {
            _timeSlowRatio.RemoveChange(slowFlag);
            SyncTimeScale();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        
        // 把合成后的 FinalTimeScale 写入 timeScale，由平台实现落到 Time.timeScale / _timeScale
        private void SyncTimeScale()
        {
            timeScale = FinalTimeScale;
        }

        // 重置到初始缩放状态（基准倍速回 1、清空全部减速登记并重新同步），供 Shutdown 调用
        protected void ResetTimeScale()
        {
            _gameSpeed = 1f;
            _timeSlowRatio.Clear();
            SyncTimeScale();
        }
    }
}
