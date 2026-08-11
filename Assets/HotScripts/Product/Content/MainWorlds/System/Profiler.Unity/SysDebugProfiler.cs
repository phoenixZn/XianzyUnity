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
        }
        
        public void Execute()
        {
            ExecuteAcc++;

            if (ExecuteAcc == 1)
            {
            }

            if (ExecuteAcc % 100 == 0)
            {
                ProfilerAttributes_Old();
            }
            if (ExecuteAcc % 100 == 10)
            {
                ProfilerAttributes_SimpleArray();
            }
            if (ExecuteAcc % 100 == 20)
            {
                ProfilerAttributes_SimpleDic();
            }
            if (ExecuteAcc % 100 == 30)
            {
                ProfilerAttributes_New();
            }
            if (ExecuteAcc % 100 == 40)
            {
                ProfilerAttributes_NewFastKey();
            }
        }
        
        public void TearDown()
        {
        }

    
    }


    
}