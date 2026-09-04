#if CONSOLE_CLIENT
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Xease
{
    /// <summary>
    /// 协程句柄（命令行宿主实现）：承载一条业务协程的运行状态，支持停止/暂停/恢复与完成回调；
    /// 无 MonoBehaviour 宿主，由 CoroutineService 的 IEnvUpdate 帧泵逐帧 Tick 驱动；
    /// 显式状态机模拟 Unity 调度语义：null=等一帧、嵌套 IEnumerator 压栈执行、yield handler 等其结束
    /// </summary>
    public class CoroutineHandler : ICoroutineHandler
    {
        // 等待状态：当前帧 Tick 时先结算等待，再决定是否推进枚举器
        private enum EWaitState
        {
            None,       // 无等待，直接推进
            WaitFrame,  // yield return null 等，等一帧
            WaitHandler // yield return CoroutineHandler，等其 Running 变 false
        }

        protected Action<CoroutineHandler> mRemoveAction;
        // 完成回调列表（参数 true=主动 Stop，false=自然结束）；不用 UnityEvent，避免 CLI 运行期依赖
        protected List<UnityAction<bool>> mCompletedActions = new List<UnityAction<bool>>();

        // 嵌套枚举器栈：yield return IEnumerator 时压栈，耗尽弹栈，对齐 Unity 嵌套协程语义
        private readonly Stack<IEnumerator> _routineStack = new Stack<IEnumerator>();
        private EWaitState _waitState = EWaitState.None;
        // WaitHandler 状态下等待的内层句柄
        private CoroutineHandler _waitHandler;
        // Finish 幂等标记：Stop() 与自然结束都可能触发 Finish
        private bool _finished;

        public ICoroutine Owner
        {
            get; private set;
        }
        public IEnumerator Coroutine
        {
            get; private set;
        }
        public bool Paused
        {
            get; private set;
        }
        public bool Running
        {
            get; private set;
        }
        public bool Stopped
        {
            get; private set;
        }

        public CoroutineHandler(ICoroutine owner, IEnumerator coroutine, Action<CoroutineHandler> mRemoveAction)
        {
            Owner = owner;
            Coroutine = coroutine;
            this.mRemoveAction = mRemoveAction;
            Start();
        }

        private void Start()
        {
            if (Running)
            {
                return;
            }
            if (Coroutine == null)
            {
                return;
            }
            Running = true;
            _routineStack.Push(Coroutine);
        }

        //////////////////////////////////////////////////////////////////////////
        /// ICoroutineHandler:

        /// <summary>
        /// 停止协程：立即终止并触发完成回调（回调参数为 true）
        /// </summary>
        public void Stop()
        {
            if (Stopped)
            {
                return;
            }
            Stopped = true;
            Running = false;
            Finish();
        }

        public void Pause()
        {
            Paused = true;
        }

        public void Resume()
        {
            Paused = false;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>
        /// 注册完成回调，参数表示是否被主动 Stop（true=停止，false=自然结束）
        /// </summary>
        public ICoroutineHandler OnCompleted(UnityAction<bool> action)
        {
            mCompletedActions.Add(action);
            return this;
        }

        // 帧泵入口：由 CoroutineService.EnvUpdate 每帧调用；dt 预留给未来的时间类等待指令
        internal void Tick(float dt)
        {
            if (!Running || Paused)
            {
                return;
            }
            if (IsWaiting())
            {
                return;
            }
            StepMoveNext();
        }

        // 结束收尾：移出服务字典、派发完成回调；幂等，重复调用无副作用
        private void Finish()
        {
            if (_finished)
            {
                return;
            }
            _finished = true;
            _waitHandler = null;
            _routineStack.Clear();
            mRemoveAction?.Invoke(this);
            for (int i = 0; i < mCompletedActions.Count; i++)
            {
                mCompletedActions[i]?.Invoke(Stopped);
            }
            mCompletedActions.Clear();
            Coroutine = null;
        }

        // 结算当前等待状态；返回 true 表示本帧仍在等待，不推进枚举器
        private bool IsWaiting()
        {
            switch (_waitState)
            {
                case EWaitState.WaitFrame:
                    // yield null 语义为「下一帧再继续」：设置等待的那一帧已结算，本帧直接放行
                    _waitState = EWaitState.None;
                    return false;
                case EWaitState.WaitHandler:
                    // 内层 handler 结束（含被 Stop）后继续推进外层
                    if (_waitHandler != null && _waitHandler.Running)
                    {
                        return true;
                    }
                    _waitHandler = null;
                    _waitState = EWaitState.None;
                    return false;
                default:
                    return false;
            }
        }

        // 推进枚举机：栈顶耗尽则弹栈继续上一层；异常必须兜底 Finish，否则 handler 泄漏、等待者永久挂起
        private void StepMoveNext()
        {
            while (Running && _routineStack.Count > 0)
            {
                IEnumerator e = _routineStack.Peek();
                bool moveNext;
                try
                {
                    moveNext = e.MoveNext();
                }
                catch (Exception exception)
                {
                    // CLI 运行期 UnityEngine.Debug 的 native 绑定不可用，必须走 GEnv 注入的日志
                    G.LogError(exception.ToString());
                    Running = false;
                    break;
                }

                if (!moveNext)
                {
                    _routineStack.Pop();
                    continue;
                }

                ApplyYieldInstruction(e.Current);
                return;
            }

            if (_routineStack.Count == 0)
            {
                Running = false;
            }
            if (!Running)
            {
                Finish();
            }
        }

        // 解释 yield 指令并设置等待状态；仅支持最小集，未知指令告警后按等一帧处理
        private void ApplyYieldInstruction(object current)
        {
            if (current == null)
            {
                _waitState = EWaitState.WaitFrame;
                return;
            }
            if (current is IEnumerator nested)
            {
                _routineStack.Push(nested);
                return;
            }
            if (current is CoroutineHandler handler)
            {
                _waitHandler = handler;
                _waitState = EWaitState.WaitHandler;
                return;
            }
            G.LogError($"CoroutineHandler: CLI 不支持的 yield 指令 {current.GetType().Name}，按等一帧处理");
            _waitState = EWaitState.WaitFrame;
        }
    }
}
#endif
