namespace Xease
{
    public class TimeService_Console : TimeServiceBase
    {
        public override void Shutdown()
        {
            ResetTimeScale();
        }

        public const float CLI_TickDt = 0.02f;
        // 平台内的时间缩放载体，由基类 SyncTimeScale 写入
        private float _timeScale = 1f;
        
        //////////////////////////////////////////////////////////////////////////
        /// ITickTimeService：
        public override float fixedDeltaTime
        {
            get => CLI_TickDt * _timeScale;
        }

        public override float fixedUnscaledDeltaTime
        {
            get => CLI_TickDt;
        }

        public override float deltaTime
        {
            get => CLI_TickDt * _timeScale;
        }

        public override float unscaledDeltaTime
        {
            get => CLI_TickDt;
        }
        
        public override float timeScale
        {
            get => _timeScale;
            protected set
            {
                _timeScale = value;
            }
        }
    }
}
