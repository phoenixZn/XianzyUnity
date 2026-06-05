using System;
using System.Collections;
using Launcher;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Xease
{
    public class CoroutineCompletedHandler : UnityEvent<bool>
    {
    }
    
    public class CoroutineHandler : CustomYieldInstruction, ICoroutineHandler
    {
        protected Action<CoroutineHandler> mRemoveAction;
        protected CoroutineCompletedHandler mCompletedAction = new CoroutineCompletedHandler();
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

        public override bool keepWaiting => Running;
        
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
                //Log.Debug("当前协程未完成");
                return;
            }
            if (Coroutine == null)
            {
                //Log.Debug("协程未指定");
                return;
            }
            Running = true;
            Object.FindObjectOfType<MonoLauncher>().StartCoroutine(CallWrapper());
        }
        
        public void Stop()
        {
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
        
        private void Finish()
        {
            mRemoveAction?.Invoke(this);
            mCompletedAction?.Invoke(Stopped);
            mCompletedAction.RemoveAllListeners();
            Coroutine = null;
        }
        
        public ICoroutineHandler OnCompleted(UnityAction<bool> action)
        {
            mCompletedAction.AddListener(action);
            return this;
        }
        
        private IEnumerator CallWrapper()
        {
            yield return null;
            IEnumerator e = Coroutine;
            while (Running)
            {
                if (Paused)
                {
                    yield return null;
                }
                else
                {
                    if (e != null && e.MoveNext())
                    {
                        yield return e.Current;
                    }
                    else
                    {
                        Running = false;
                    }
                }
            }
            Finish();
        }
    }
}