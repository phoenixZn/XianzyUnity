using Entitas;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler : ECWorldSystem, IInitializeSystem, IExecuteSystem, ITearDownSystem
    {
        private int ExecuteAcc = 0;
        
        public SysDebugProfiler(ECWorlds worlds) : base(worlds)
        {
        }

        public void Initialize()
        {
            ExecuteAcc = 0;
            InitAttributes();
            InitSvcTimer();
        }
        
        public void Execute()
        {
            ExecuteAcc++;

            if (ExecuteAcc == 1)
            {
            }

            ProfilerSvcTimer();
            //ProfilerExecute_Attributes();
        }



        public void TearDown()
        {
            TearDownSvcTimer();
        }

    
    }


    
}