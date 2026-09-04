using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Xease
{
    public class CoroutineCompletedHandler : UnityEvent<bool>
    {
    }

    /// <summary>
    /// 协程句柄：承载一条业务协程的运行状态，支持停止/暂停/恢复与完成回调；
    /// 自身是 CustomYieldInstruction，可在其他协程中 yield 等待其结束
    /// </summary>
    public class CoroutineHandler : CustomYieldInstruction, ICoroutineHandler
    {
        // 协程运行的 MonoBehaviour 宿主，由服务构造时注入（通常为 GameEntry）
        private readonly MonoBehaviour _host;

        protected Action<CoroutineHandler> mRemoveAction;
        protected CoroutineCompletedHandler mCompletedAction = new CoroutineCompletedHandler();

        // 底层实体协程引用，Stop 时用于真正终止，避免外壳协程空转到内层 yield 结束
        private Coroutine _wrapperCoroutine;
        // Finish 幂等标记：Stop() 与 wrapper 自然退出都可能触发 Finish
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

        //////////////////////////////////////////////////////////////////////////
        /// CustomYieldInstruction：override

        public override bool keepWaiting => Running;

        public CoroutineHandler(ICoroutine owner, IEnumerator coroutine, Action<CoroutineHandler> mRemoveAction, MonoBehaviour host)
        {
            Owner = owner;
            Coroutine = coroutine;
            this.mRemoveAction = mRemoveAction;
            _host = host;
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
            if (_host == null)
            {
                // 宿主未注入（GEnvParam.UnityHost 未赋值）时拒绝启动，避免 NRE 静默失败
                Debug.LogError("CoroutineHandler: 协程宿主为空，协程无法启动");
                return;
            }
            Running = true;
            _wrapperCoroutine = _host.StartCoroutine(CallWrapper());
        }

        //////////////////////////////////////////////////////////////////////////
        /// ICoroutineHandler:

        /// <summary>
        /// 停止协程：立即终止底层实体协程并触发完成回调（回调参数为 true）
        /// </summary>
        public void Stop()
        {
            if (Stopped)
            {
                return;
            }
            Stopped = true;
            Running = false;
            if (_wrapperCoroutine != null && _host != null)
            {
                _host.StopCoroutine(_wrapperCoroutine);
            }
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
            mCompletedAction.AddListener(action);
            return this;
        }

        // 结束收尾：移出服务字典、派发完成回调；幂等，重复调用无副作用
        private void Finish()
        {
            if (_finished)
            {
                return;
            }
            _finished = true;
            _wrapperCoroutine = null;
            mRemoveAction?.Invoke(this);
            mCompletedAction?.Invoke(Stopped);
            mCompletedAction.RemoveAllListeners();
            Coroutine = null;
        }

        // 外壳协程：驱动业务协程逐帧推进；异常必须兜底 Finish，否则 handler 泄漏、等待者永久挂起
        private IEnumerator CallWrapper()
        {
            IEnumerator e = Coroutine;
            while (Running)
            {
                if (Paused)
                {
                    yield return null;
                    continue;
                }

                bool moveNext;
                try
                {
                    moveNext = e != null && e.MoveNext();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Running = false;
                    break;
                }

                if (moveNext)
                {
                    // 内层 yield 的指令（WaitForSeconds 等）原样转交给 Unity 调度器
                    yield return e.Current;
                }
                else
                {
                    Running = false;
                }
            }
            Finish();
        }
    }
}
