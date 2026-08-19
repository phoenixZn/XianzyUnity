#if !CONSOLE_CLIENT
using Unity.Profiling;
using Xease;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler
    {
        //////////////////////////////////////////////////////////////////////////
        /// Debug Action:
        
        // 高频档：10/20/50ms 均分
        public const int TimerHighFreqCount = 1000;
        // 中频档：100/200/500ms 均分
        public const int TimerMidFreqCount = 1000;
        // 低频档：1s/2s/5s 均分
        public const int TimerLowFreqCount = 1000;

        // 两实现共用的间隔表，保证规格一致
        private static readonly float[] s_timerHighFreqIntervals = { 0.5f, };
        private static readonly float[] s_timerMidFreqIntervals = { 0.1f, 0.2f, 0.3f };
        private static readonly float[] s_timerLowFreqIntervals = { 1f, 2f, 3f, 4f };

        private static readonly ProfilerMarker s_timerSvcEnvUpdateMarker = new("SvcTimer.Tick");
        private static readonly ProfilerMarker s_gameTimerEnvUpdateMarker = new("SvcTimer.Old.Tick");
        private static readonly ProfilerMarker s_timerSvcFireMarker = new("SvcTimer.FireCall");
        private static readonly ProfilerMarker s_gameTimerFireMarker = new("SvcTimer.Old.FireCall");
        
        // 本地 TimerService 实例，不走 G.Timer
        private TimerService _timerSvc;
        // 对照实现：列表扫描式 GameTimerManager
        private GameTimerManager _gameTimerMgr;
        // TimerService 回调轻量计数
        private int _timerSvcFireCount;
        // GameTimerManager 回调轻量计数
        private int _gameTimerFireCount;
        // 中低频单次 Timer 补批累计（秒）；满 1s 再 Fill 一批
        private float _timerFillAcc;

        private void InitSvcTimer()
        {
            TearDownSvcTimer();
            _timerSvc = new TimerService();
            _timerSvcFireCount = 0;
            
            _gameTimerMgr = new GameTimerManager();
            _gameTimerMgr.Init();
            _gameTimerFireCount = 0;
            _timerFillAcc = 0f;

            // 高频：只在初始化挂无限循环
            FillHighFreqTimers(_timerSvc, OnTimerSvcFire);
            FillHighFreqTimers(_gameTimerMgr, OnGameTimerFire);
            // 中低频：立刻补一批单次 Timer，之后每秒再补
            FillIdenticalTimers(_timerSvc, OnTimerSvcFire);
            FillIdenticalTimers(_gameTimerMgr, OnGameTimerFire);
        }

        private void ProfilerExecute_SvcTimer(float dt, float dt_unscaled)
        {
            TryRefillOneShotTimers(dt);
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
        // 定期补一批Timer
        private void TryRefillOneShotTimers(float dt)
        {
            _timerFillAcc += dt;
            if (_timerFillAcc < 0.1f)
            {
                return;
            }
            _timerFillAcc -= 0.1f;
            if (_timerSvc != null)
            {
                FillIdenticalTimers(_timerSvc, OnTimerSvcFire);
            }
            if (_gameTimerMgr != null)
            {
                FillIdenticalTimers(_gameTimerMgr, OnGameTimerFire);
            }
        }

        // 高频无限循环：仅初始化调用（repeatCount = -1）
        private static void FillHighFreqTimers(ITimerService timer, System.Action<int> callback)
        {
            AddTimers(timer, callback, TimerHighFreqCount, s_timerHighFreqIntervals, -1);
        }

        // 中低频单次：每秒调用一次（repeatCount = 1）
        private static void FillIdenticalTimers(ITimerService timer, System.Action<int> callback)
        {
            AddTimers(timer, callback, TimerMidFreqCount, s_timerMidFreqIntervals, 1);
            AddTimers(timer, callback, TimerLowFreqCount, s_timerLowFreqIntervals, 1);
        }

        // 按间隔表轮询注册 Timer（useUnscaled = false）
        private static void AddTimers(ITimerService timer, System.Action<int> callback, int count, float[] intervalSecs, int repeatCount)
        {
            for (int i = 0; i < count; i++)
            {
                float intervalSec = intervalSecs[i % intervalSecs.Length];
                timer.AddTimer(callback, intervalSec, repeatCount, false);
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
#endif
