using System.Collections;

namespace Xease
{

    //////////////////////////////////////////////////////////////////////////
    // Service: 协程
    public interface ICoroutineHandler
    {
        void Stop();
        void Pause();
        void Resume();
    }
    
    public interface ICoroutineService : IService
    {
        /// <summary>
        /// 启动协程，返回可用于停止/暂停/恢复与完成回调的句柄
        /// </summary>
        /// <remarks>
        /// 协程内 yield return 另一个 IEnumerator 会被 Unity 作为嵌套协程执行，但嵌套段
        /// 不受 Pause 控制，且其异常会绕过服务兜底导致协程链终止、OnCompleted 不触发。
        /// 需要受控嵌套时，请 yield return 本服务 StartCoroutine 返回的 handler
        /// （默认实现 CoroutineHandler 继承 CustomYieldInstruction，Unity 会等待其结束；
        /// 若 handler 实现不含 CustomYieldInstruction，yield 它不会等待）。
        /// </remarks>
        /// <example>
        /// <code>
        /// IEnumerator OuterRoutine()
        /// {
        ///     // 方式一：直接嵌套。可运行，外层等其完成；
        ///     // 但嵌套段不受 Pause 控制，其异常无兜底
        ///     yield return OtherRoutine();
        ///
        ///     // 方式二（推荐）：通过服务启动，返回的 handler 可直接 yield。
        ///     // 注意：外层 handler 的 Pause/Stop 控制不了嵌套协程，
        ///     // 必须持有内层 handler 才能对它 Pause/Stop
        ///     var inner = G.Coroutines.StartCoroutine(OtherRoutine());
        ///     yield return inner;
        ///     // inner.Pause(); inner.Resume(); inner.Stop(); 可随时独立调用
        /// }
        /// </code>
        /// </example>
        ICoroutineHandler StartCoroutine(IEnumerator coroutine);
        /// <summary>
        /// 以指定 owner 启动协程，owner 可用于批量停止（StopOwnerCoroutines）
        /// </summary>
        /// <remarks>
        /// 协程内 yield return 另一个 IEnumerator 会被 Unity 作为嵌套协程执行，但嵌套段
        /// 不受 Pause 控制，且其异常会绕过服务兜底导致协程链终止、OnCompleted 不触发。
        /// 需要受控嵌套时，请 yield return 本服务 StartCoroutine 返回的 handler
        /// （默认实现 CoroutineHandler 继承 CustomYieldInstruction，Unity 会等待其结束；
        /// 若 handler 实现不含 CustomYieldInstruction，yield 它不会等待）。
        /// </remarks>
        /// <example>
        /// <code>
        /// IEnumerator OuterRoutine()
        /// {
        ///     // 方式一：直接嵌套。可运行，外层等其完成；
        ///     // 但嵌套段不受 Pause 控制，其异常无兜底
        ///     yield return OtherRoutine();
        ///
        ///     // 方式二（推荐）：通过服务启动，返回的 handler 可直接 yield。
        ///     // 注意：外层 handler 的 Pause/Stop 控制不了嵌套协程，
        ///     // 必须持有内层 handler 才能对它 Pause/Stop
        ///     var inner = G.Coroutines.StartCoroutine(owner, OtherRoutine());
        ///     yield return inner;
        ///     // inner.Pause(); inner.Resume(); inner.Stop(); 可随时独立调用
        /// }
        /// </code>
        /// </example>
        ICoroutineHandler StartCoroutine(object owner, IEnumerator coroutine);

        void StopOwnerCoroutines(object owner);
        void StopAllCoroutines();
    }
}