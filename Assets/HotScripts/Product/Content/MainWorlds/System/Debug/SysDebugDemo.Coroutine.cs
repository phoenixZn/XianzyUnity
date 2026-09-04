using System.Collections;
using System.Collections.Generic;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugDemo
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>
        /// 协程服务测试套件：启动编排协程顺序跑「嵌套+正常结束回调 / 暂停恢复 / 停止」三用例；
        /// 只用 G.Coroutines 公共 API 与最简 yield 集（null / handler），Unity 与 CLI 双环境可跑
        /// </summary>
        /// <remarks>
        /// 时序断言只用相对比较：Unity 的 StartCoroutine 首段同步执行、CLI 首 Tick 在下一帧，
        /// 且 CLI 帧泵倒序遍历，跨协程的启动先后顺序两个环境不一致，不断言绝对帧数
        /// </remarks>
        private void TestCoroutineSuite()
        {
            G.Coroutines.StartCoroutine(CoroutineSuiteOrchestrator());
        }

        // 编排协程：顺序执行三个用例，最后输出总结
        private IEnumerator CoroutineSuiteOrchestrator()
        {
            yield return CaseNestedComplete();
            yield return CasePauseResume();
            yield return CaseStop();
            G.Log("TestCoroutineSuite: finished, check logs above for ok/fail");
        }

        // 用例 1：嵌套协程 + 正常结束 + 完成回调（stopped 应为 false）
        private IEnumerator CaseNestedComplete()
        {
            // 执行顺序记录，用于验证外层确实等到内层结束后才继续
            var order = new List<string>();
            bool completedFired = false;
            bool stoppedFlag = true;

            IEnumerator Inner()
            {
                order.Add("inner-1");
                yield return null;
                order.Add("inner-2");
            }

            IEnumerator Outer(ICoroutineHandler inner)
            {
                order.Add("outer-1");
                // yield handler：等内层协程结束（Unity 走 CustomYieldInstruction，CLI 走 WaitHandler 状态）
                yield return inner;
                order.Add("outer-after-inner");
            }

            var innerHandler = G.Coroutines.StartCoroutine(Inner());
            var outerHandler = G.Coroutines.StartCoroutine(Outer(innerHandler));
            outerHandler.OnCompleted(stopped =>
            {
                completedFired = true;
                stoppedFlag = stopped;
            });

            yield return outerHandler;

            int idxInner2 = order.IndexOf("inner-2");
            int idxOuterAfter = order.IndexOf("outer-after-inner");
            bool nestedOk = idxInner2 >= 0 && idxOuterAfter > idxInner2;
            if (!nestedOk)
                G.LogError($"TestCoroutineSuite nested fail: order=[{string.Join(",", order)}]");
            else
                G.Log("TestCoroutineSuite nested ok");

            if (!completedFired || stoppedFlag)
                G.LogError($"TestCoroutineSuite complete-callback fail: fired={completedFired}, stopped={stoppedFlag}");
            else
                G.Log("TestCoroutineSuite complete-callback ok");
        }

        // 用例 2：暂停期间计数冻结，恢复后继续增长
        private IEnumerator CasePauseResume()
        {
            int counter = 0;
            IEnumerator Counting()
            {
                while (true)
                {
                    counter++;
                    yield return null;
                }
            }

            var handler = G.Coroutines.StartCoroutine(Counting());
            yield return WaitFrames(2);
            int beforePause = counter;

            handler.Pause();
            yield return WaitFrames(3);
            // 暂停 3 帧计数必须冻结
            bool frozenOk = counter == beforePause;

            handler.Resume();
            yield return WaitFrames(2);
            // 恢复后计数必须重新增长
            bool resumedOk = counter > beforePause;

            // 清理无限协程，防止泄漏到后续帧
            handler.Stop();

            if (!frozenOk)
                G.LogError($"TestCoroutineSuite pause fail: beforePause={beforePause}, duringPause={counter}");
            else
                G.Log("TestCoroutineSuite pause ok");
            if (!resumedOk)
                G.LogError($"TestCoroutineSuite resume fail: beforePause={beforePause}, afterResume={counter}");
            else
                G.Log("TestCoroutineSuite resume ok");
        }

        // 用例 3：主动停止触发回调（stopped 应为 true）且协程不再推进到末尾
        private IEnumerator CaseStop()
        {
            bool completedFired = false;
            bool stoppedFlag = false;
            bool reachedEnd = false;

            IEnumerator Long()
            {
                yield return null;
                yield return null;
                // 正常应在中途被 Stop，不应到达
                reachedEnd = true;
            }

            var handler = G.Coroutines.StartCoroutine(Long());
            handler.OnCompleted(stopped =>
            {
                completedFired = true;
                stoppedFlag = stopped;
            });

            yield return WaitFrames(1);
            handler.Stop();
            yield return WaitFrames(1);

            if (!completedFired || !stoppedFlag || reachedEnd)
                G.LogError($"TestCoroutineSuite stop fail: fired={completedFired}, stopped={stoppedFlag}, reachedEnd={reachedEnd}");
            else
                G.Log("TestCoroutineSuite stop ok");
        }

        // 帧等待辅助：等 n 帧后继续
        private static IEnumerator WaitFrames(int n)
        {
            for (int i = 0; i < n; i++)
            {
                yield return null;
            }
        }
    }
}
