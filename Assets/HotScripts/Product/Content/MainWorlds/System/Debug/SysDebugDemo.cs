using Cysharp.Threading.Tasks;
using Entitas;
using LitMotion;

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
                TestRandomRange();
                SmokeUniTaskYield();
                SmokeLMotion();
            }
        }
        
        public void TearDown()
        {

        }

        // Yield 走线程池 / SynchronizationContext（CLI）或 PlayerLoop（Unity），不阻塞 Execute
        private void SmokeUniTaskYield()
        {
            UniTask.Void(async () =>
            {
                await UniTask.Yield();
                G.Log("UniTask: Yield completed.");
            });
        }

        // CLI 依赖 GameEntry 已切 Manual 并泵时间；Unity 走默认 PlayerLoop Scheduler
        private void SmokeLMotion()
        {
            LMotion.Create(0f, 100f, 0.4f)
                .WithEase(Ease.OutCubic)
                .WithOnComplete(() => G.Log("LMotion OnComplete"))
                .Bind(v => G.Log($"LMotion v={v}"));
        }

    }
}
