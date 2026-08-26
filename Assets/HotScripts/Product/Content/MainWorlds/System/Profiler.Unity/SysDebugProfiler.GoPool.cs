using Cysharp.Threading.Tasks;
using UnityEngine;
using Xease;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler
    {
        //////////////////////////////////////////////////////////////////////////
        /// Debug Action:

        // 周期租还 Demo 预制体，并定期清空 ActorSphere 池
        private void ProfilerExecute_GoPool()
        {
            if (ExecuteAcc % 10 == 1)
            {
                RentDemoGoAsync("ActorCube").Forget();
            }
            if (ExecuteAcc % 10 == 5)
            {
                RentDemoGoAsync("ActorSphere");
            }
            if (ExecuteAcc % 100 == 50)
            {
                G.GameObjectPool_Core.Clear("ActorSphere");
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        // Execute 周期触发：异步租预制体，摆到位姿后再激活（Rent 交出未激活实例）
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
