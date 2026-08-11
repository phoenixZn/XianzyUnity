using Unity.Profiling;
using Xease;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler
    {
        //////////////////////////////////////////////////////////////////////////
        /// Debug Action:

        // 帧间固定步进，保证 Profiler 采样可比（约 60fps）
        private const float TimerProfilerDt = 1f / 60f;

        // 无限循环 Timer 总数（高频 + 中频 + 低频）
        public const int TimerTotalCount = 2000;
        // 高频档：10/20/50ms 均分
        public const int TimerHighFreqCount = 400;
        // 中频档：100/200/500ms 均分
        public const int TimerMidFreqCount = 800;
        // 低频档：1s/2s/5s 均分
        public const int TimerLowFreqCount = 800;

        // 与 AddInfiniteTimers 共用的间隔表，保证两实现规格一致
        private static readonly float[] s_timerHighFreqIntervals = { 0.01f, 0.02f, 0.05f };
        private static readonly float[] s_timerMidFreqIntervals = { 0.1f, 0.2f, 0.5f };
        private static readonly float[] s_timerLowFreqIntervals = { 1f, 2f, 5f };

        private static readonly ProfilerMarker s_timerSvcEnvUpdateMarker = new("TimerService.EnvUpdate");
        private static readonly ProfilerMarker s_gameTimerEnvUpdateMarker = new("GameTimerManager.EnvUpdate");

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

        private void ProfilerSvcTimer()
        {
            if (_timerSvc != null)
            {
                using (s_timerSvcEnvUpdateMarker.Auto())
                {
                    _timerSvc.EnvUpdate(TimerProfilerDt, TimerProfilerDt);
                }
            }

            if (_gameTimerMgr != null)
            {
                using (s_gameTimerEnvUpdateMarker.Auto())
                {
                    _gameTimerMgr.EnvUpdate(TimerProfilerDt, TimerProfilerDt);
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
                timer.AddTimer(callback, intervalSec, -1, false);
            }
        }

        // TimerService：仅做计数，测量调度开销
        private void OnTimerSvcFire(int execCount)
        {
            _timerSvcFireCount++;
        }

        // GameTimerManager：仅做计数，测量调度开销
        private void OnGameTimerFire(int execCount)
        {
            _gameTimerFireCount++;
        }
    }
}
