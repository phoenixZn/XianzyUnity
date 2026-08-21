using Entitas;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler : ECWorldSystem, IInitializeSystem, IExecuteSystem, ITearDownSystem, IUpdateSystem
    {
        private int ExecuteAcc = 0;
        
        public SysDebugProfiler(ECWorlds worlds) : base(worlds)
        {
        }

#if CONSOLE_CLIENT
        // 纯 C# 工程不采样 Profiler；Unity 侧实现见 Profiler.Unity 分部
        public void Initialize()
        {
        }

        public void Execute()
        {
            ExecuteAcc++;
        }

        public void Update(float dt, float dt_unscaled)
        {
        }

        public void TearDown()
        {
        }
#else
        public void Initialize()
        {
            ExecuteAcc = 0;
            //InitAttributes();
            //InitSvcTimer();
        }
        
        public void Execute()
        {
            ExecuteAcc++;
            //ProfilerExecute_Attributes();
        }
        
        public void Update(float dt, float dt_unscaled)
        {
            //ProfilerExecute_SvcTimer(dt, dt_unscaled);
        }

        public void TearDown()
        {
            TearDownSvcTimer();
        }
#endif
    }
}
