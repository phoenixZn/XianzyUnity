using System.Collections.Generic;
using Entitas;
using Unity.Profiling;

namespace Xease.CoreGame
{
    public partial class SysDebugDemo : ECWorldSystem, IInitializeSystem, IExecuteSystem, ITearDownSystem
    {
        private int ExecuteAcc = 0;
        
        public SysDebugDemo(ECWorlds worlds) : base(worlds)
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
        }
        
        public void TearDown()
        {
        }

    
    }


    
}