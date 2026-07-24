using System;
using System.Collections.Generic;

namespace Xease
{
    public class ValueEventService : IValueEventService
    {
        // 按事件类型存放监听集合
        private readonly Dictionary<Type, HashSet<Delegate>> _typedHandlers = new ();

        // 分发嵌套深度；>0 时增删入延迟队列，避免遍历中改集合
        private int _dispatchDepth = 0;
        private readonly Queue<KeyValuePair<Type, Delegate>> _deferredAddHandlers = new ();
        private readonly Queue<KeyValuePair<Type, Delegate>> _deferredRemoveHandlers = new ();
        
        //////////////////////////////////////////////////////////////////////////
        /// IService
        public void Shutdown()
        {
            _typedHandlers.Clear();
            _deferredAddHandlers.Clear();
            _deferredRemoveHandlers.Clear();
            _dispatchDepth = 0;
        }
        
        //////////////////////////////////////////////////////////////////////////
        /// IValueEventService
        public void Dispatch<T>(T evt) where T : struct, IValueEvent
        {
            // 类型安全分发
            DispatchTyped(evt);
        }

        private void DispatchTyped<T>(T evt) where T : struct, IValueEvent
        {
            Type eventType = typeof(T);

            _dispatchDepth++;
            try
            {
                if (_typedHandlers.TryGetValue(eventType, out var handlerList))
                {
                    foreach (var handler in handlerList)
                    {
                        ((Action<T>)handler)(evt);
                    }
                }
            }
            finally
            {
                _dispatchDepth--;
                if (_dispatchDepth == 0)
                {
                    HandleDeferredHandlers();
                }
            }
        }

        private void HandleDeferredHandlers()
        {
            while (_deferredAddHandlers.TryDequeue(out var kv))
            {
                AddHandler(kv.Key, kv.Value);
            }
            while (_deferredRemoveHandlers.TryDequeue(out var kv))
            {
                RemoveHandler(kv.Key, kv.Value);
            }
        }

        public void AddHandler<T>(Action<T> handler) where T : struct, IValueEvent
        {
            Type type = typeof(T);
            if (_dispatchDepth > 0)
            {
                _deferredAddHandlers.Enqueue(new KeyValuePair<Type, Delegate>(type, handler));
                return;
            }
            AddHandler(type, handler);
        }

        private void AddHandler(Type type, Delegate handler)
        {
            if (!_typedHandlers.TryGetValue(type, out var handlers))
            {
                handlers = new ();
                _typedHandlers[type] = handlers;
            }

            bool r = handlers.Add(handler);
            if (!r)
            {
                G.LogError("AddHandler Error");
            }
        }

        public void RemoveHandler<T>(Action<T> handler) where T : struct, IValueEvent
        {
            Type type = typeof(T);
            if (_dispatchDepth > 0)
            {
                _deferredRemoveHandlers.Enqueue(new KeyValuePair<Type, Delegate>(type, handler));
                return;
            }
            RemoveHandler(type, handler);
        }

        private void RemoveHandler(Type type, Delegate handler)
        {
            if (_typedHandlers.TryGetValue(type, out var handlers))
            {
                handlers.Remove(handler);

                if (handlers.Count == 0)
                {
                    _typedHandlers.Remove(type);
                }
            }
        }
        
    }
}
