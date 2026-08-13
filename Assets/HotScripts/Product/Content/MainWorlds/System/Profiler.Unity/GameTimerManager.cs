using System;
using System.Collections.Generic;

namespace Xease.CoreGame.Debug
{
    using GameTimerCallback = Action<float, int>;
    
    public class GameTimerManager : ITimerService, IEnvUpdate
    {
        protected class Timer
        {
            public int id;
            public float interval;
            public float nextCallbackTime;
            public Action<int> callback;
            public int times;
            public int curTimes;
            public bool changeWithTimeDilation;
        }
        
        protected float flexCurTime = 0.0f; // 考虑时间缩放的当前时间
        protected float fixedCurTime = 0.0f; // 不考虑时间缩放的当前时间

        private int nextTimerId = 1;
        protected bool isTicking = false;
        protected List<Timer> timerList = new(64); // 当前持有的timer列表
        protected List<Timer> cacheAddTimerList = new(16); // 缓存添加列表
        protected List<int> cacheRemoveTimerList = new(16); // 缓存删除列表

        public GameTimerManager()
        {
            
        }

        public void Init()
        {
            InitTimerPool();
        }

        #region 池相关

		protected int timerPoolCapacity = 16;
		protected Stack<Timer> simpleTimerPool;

		protected void InitTimerPool()
		{
			simpleTimerPool = new(timerPoolCapacity);
		}

        public void SetTimerPoolCapactiy(int capacity)
        {
            timerPoolCapacity = capacity;
        }

		protected Timer GetTimer()
        {
            if (simpleTimerPool.Count > 0)
                return simpleTimerPool.Pop();
            return new Timer();
        }

        protected void PutTimer(Timer timer)
        {
            timer.id = 0;
            timer.interval = 0f;
            timer.nextCallbackTime = 0f;
            timer.callback = null;
            timer.times = 0;
            timer.curTimes = 0;
            timer.changeWithTimeDilation = false;

            if (simpleTimerPool.Count < timerPoolCapacity)
                simpleTimerPool.Push(timer);
        }
        
#endregion

        public int AddTimer(Action<int> cb, float interval = 1.0f, int times = 1, bool useTimeScale = false)
        {
            if (cb == null)
            {
                KLogger.LogError($"[GameTimerManager] callback should not be null");
                return -1;
            }
            
            var timer = GetTimer();
            timer.id = nextTimerId++;
            timer.interval = interval;
            timer.nextCallbackTime = GetCurTime(useTimeScale) + interval;
            timer.callback = cb;
            timer.times = times;
            timer.curTimes = 0;
            timer.changeWithTimeDilation = useTimeScale;
            
            if (isTicking)
                cacheAddTimerList.Add(timer);
            else
                timerList.Add(timer);

            return timer.id;
        }
        

        bool ITimerService.RemoveTimer(int timerId)
        {
            return RemoveTimer(timerId) >= 0;
        }

        public int RemoveTimer(int id)
        {
            if (isTicking)
            {
                cacheRemoveTimerList.Add(id);
                return 1;
            }

            for (int i = 0; i < timerList.Count; ++i)
            {
                if (timerList[i].id == id)
                {
                    PutTimer(timerList[i]);
                    timerList.RemoveAt(i);
                    return 0;
                }
            }

            return -1;
        }

        public void SetTimerInterval(int id, float interval)
        {
            for (int i = 0; i < timerList.Count; ++i)
            {
                var timer = timerList[i];
                if (timer.id == id)
                {
                    timer.interval = interval;
                    timer.nextCallbackTime = GetCurTime(timer.changeWithTimeDilation) + interval;
                    return;
                }
            }
        }

        public void ResetTimer(int id, float interval = -1.0f)
        {
            for (int i = 0; i < timerList.Count; ++i)
            {
                var timer = timerList[i];
                if (timer.id == id)
                {
                    if (interval >= 0.0f)
                        timer.interval = interval;
                    timer.nextCallbackTime = GetCurTime(timer.changeWithTimeDilation) + timer.interval;
                    return;
                }
            }
        }

        public void Tick(float fixedDeltaTime, float flexDeltaTime)
        {
            fixedCurTime += fixedDeltaTime;
            flexCurTime += flexDeltaTime;

            isTicking = true;

            for (int i = 0; i < timerList.Count;)
            {
                var timer = timerList[i];
                var targetTime = timer.changeWithTimeDilation ? fixedCurTime : flexCurTime;
                if (timer.nextCallbackTime <= targetTime)
                {
                    // 这里的deltaTime是这次真正触发到上次触发的时间间隔
                    try
                    {
                        timerList[i].callback(++timer.curTimes);
                    }
                    catch (Exception ex)
                    {
                        KLogger.LogError($"[GameTimerManager] Timer ID = {timer.id} callback exception: {ex}");
                    }
                    
                    if (timer.times > 0 && timer.curTimes >= timer.times)
                    {
                        PutTimer(timer);
                        timerList.RemoveAt(i);
                        continue;
                    }
                    
                    if (timer.nextCallbackTime <= targetTime)
                    {
                        timer.nextCallbackTime = targetTime + timer.interval;
                    }
                    
                    ++i;
                }
                else
                {
                    ++i;
                }
            }

            isTicking = false;

            timerList.AddRange(cacheAddTimerList);
            cacheAddTimerList.Clear();

            for (int i = 0; i < cacheRemoveTimerList.Count; ++i)
            {
                RemoveTimer(cacheRemoveTimerList[i]);
            }
            cacheRemoveTimerList.Clear();
        }

        public void Dispose()
        {
            if (isTicking)
            {
                for (int i = 0; i < timerList.Count; ++i)
                {
                    cacheRemoveTimerList.Add(timerList[i].id);
                }
                // tick中只清已经进入tick的，待添加的先不清。这样规则上统一一点，这一帧内的添加都会成功，不受AddTimer和Clear的调用时序影响
                return;
            }

            for (int i = 0; i < timerList.Count; ++i)
            {
                PutTimer(timerList[i]);
            }
            timerList.Clear();

            for (int i = 0; i < cacheAddTimerList.Count; ++i)
            {
                PutTimer(cacheAddTimerList[i]);
            }
            cacheAddTimerList.Clear();

            cacheRemoveTimerList.Clear();
        }

        protected float GetCurTime(bool changeWithTimeDilation) => changeWithTimeDilation ? fixedCurTime : flexCurTime;

        public string GenerateDebugStr()
        {
            return $"Cur timer count: {timerList.Count}";
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }

        public void EnvUpdate(float dt, float dt_unscaled)
        {
            Tick(dt, dt_unscaled);
        }
    }
}