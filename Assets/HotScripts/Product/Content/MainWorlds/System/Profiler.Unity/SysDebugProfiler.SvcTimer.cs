using Unity.Profiling;
using Xease;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler
    {
        //////////////////////////////////////////////////////////////////////////
        /// Debug Action:
        
        // 高频档：10/20/50ms 均分
        public const int TimerHighFreqCount = 10000;
        // 中频档：100/200/500ms 均分
        public const int TimerMidFreqCount = 0;
        // 低频档：1s/2s/5s 均分
        public const int TimerLowFreqCount = 0;

        // 与 AddInfiniteTimers 共用的间隔表，保证两实现规格一致
        private static readonly float[] s_timerHighFreqIntervals = { 1f, };
        private static readonly float[] s_timerMidFreqIntervals = { 0.1f, 0.2f, 0.3f };
        private static readonly float[] s_timerLowFreqIntervals = { 1f, 2f, 3f, 4f };

        private static readonly ProfilerMarker s_timerSvcEnvUpdateMarker = new("TimerService.Tick");
        private static readonly ProfilerMarker s_gameTimerEnvUpdateMarker = new("TimerService_Old.Tick");
        private static readonly ProfilerMarker s_timerSvcFireMarker = new("TimerService.FireCall");
        private static readonly ProfilerMarker s_gameTimerFireMarker = new("TimerService_Old.FireCall");
        
        // 本地 TimerService 实例，不走 G.Timer
        private TimerService _timerSvc;
        // 对照实现：列表扫描式 GameTimerManager
        private GameTimerManager _gameTimerMgr;
        // TimerService 回调轻量计数
        private int _timerSvcFireCount;
        // GameTimerManager 回调轻量计数
        private int _gameTimerFireCount;

        private void InitSvcTimer()
        {
            TearDownSvcTimer();

            _timerSvc = new TimerService();
            _timerSvcFireCount = 0;
            FillIdenticalTimers(_timerSvc, OnTimerSvcFire);

            _gameTimerMgr = new GameTimerManager();
            _gameTimerMgr.Init();
            _gameTimerFireCount = 0;
            FillIdenticalTimers(_gameTimerMgr, OnGameTimerFire);
        }

        private void ProfilerSvcTimer(float dt, float dt_unscaled)
        {
            dt = 0.01f;
            dt_unscaled = 0.01f;
            if (_timerSvc != null)
            {
                using (s_timerSvcEnvUpdateMarker.Auto())
                {
                    _timerSvc.EnvUpdate(dt, dt_unscaled);
                }
            }

            if (_gameTimerMgr != null)
            {
                using (s_gameTimerEnvUpdateMarker.Auto())
                {
                    _gameTimerMgr.EnvUpdate(dt, dt_unscaled);
                }
            }
        }

        private void TearDownSvcTimer()
        {
            if (_timerSvc != null)
            {
                _timerSvc.Shutdown();
                _timerSvc = null;
            }

            if (_gameTimerMgr != null)
            {
                _gameTimerMgr.Dispose();
                _gameTimerMgr = null;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        // 两实现共用同一套数量/间隔/无限循环规格
        private static void FillIdenticalTimers(ITimerService timer, System.Action<int> callback)
        {
            AddInfiniteTimers(timer, callback, TimerHighFreqCount, s_timerHighFreqIntervals);
            AddInfiniteTimers(timer, callback, TimerMidFreqCount, s_timerMidFreqIntervals);
            AddInfiniteTimers(timer, callback, TimerLowFreqCount, s_timerLowFreqIntervals);
        }

        // 按间隔表轮询注册无限循环 Timer（repeatCount = -1，useUnscaled = false）
        private static void AddInfiniteTimers(ITimerService timer, System.Action<int> callback, int count, float[] intervalSecs)
        {
            for (int i = 0; i < count; i++)
            {
                float intervalSec = intervalSecs[i % intervalSecs.Length];
                timer.AddTimer(callback, intervalSec, 20, false);
            }
        }

        // TimerService：仅做计数，测量调度开销
        private void OnTimerSvcFire(int execCount)
        {
            using (s_timerSvcFireMarker.Auto())
            {
                _timerSvcFireCount++;
            }
            
        }

        // GameTimerManager：仅做计数，测量调度开销
        private void OnGameTimerFire(int execCount)
        {
            using (s_gameTimerFireMarker.Auto())
            {
                _gameTimerFireCount++;
            }
        }
    }
}
