using Entitas;

namespace Xease.CoreGame.Debug
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
        }
        
        public void Execute()
        {
            ExecuteAcc++;

            if (ExecuteAcc == 1)
            {
            }
            if (ExecuteAcc % 100 == 0)
            {
            }
        }
        
        public void TearDown()
        {
        }

    
    }


    
}