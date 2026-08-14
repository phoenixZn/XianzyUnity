using Cysharp.Threading.Tasks;
using Entitas;
using UnityEngine;

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
            if (ExecuteAcc % 20 == 1)
            {
                RentDemoGoAsync("ActorCube").Forget();
            }
            if (ExecuteAcc % 20 == 10)
            {
                RentDemoGoAsync("ActorSphere").Forget();
            }
            if (ExecuteAcc % 100 == 50)
            {
            }

        }
        
        public void TearDown()
        {

        }

        // Execute 首帧触发：异步租 ActorCube，摆到位姿后再激活（Rent 交出未激活实例）
        private async UniTaskVoid RentDemoGoAsync(string path)
        {
            var testGo = await G.GameObjectPool_Core.RentAsync(path);
            
            var x = G.Random.RandFloat(-6f, 6f);
            var y = G.Random.RandFloat(-8f, 8f);
            var z = 10;
            var ry = G.Random.RandFloat(-90f, 90f);
            testGo.SetPosition(new Vector3(x, y, z))
                .SetRotation(Quaternion.Euler(0, ry, 0))
                .SetActiveState(true)
                .SetScale(Vector3.one);
            
            var life = G.Random.RandFloat(2f, 3f);
            G.Timer.AddTimer(c =>
            {
                testGo?.SetScale(Vector3.one * 1.5f);
                G.Timer.AddTimer(c =>
                {
                    G.GameObjectPool_Core.Return(testGo);
                    testGo = null;
                }, life);
            }, 1);

        }

    }
}