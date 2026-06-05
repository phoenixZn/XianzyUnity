using System;
using System.Collections.Generic;

namespace Xease
{
    
    public class ValueEventService : IValueEventService
    {
        // 双重存储结构
        private readonly Dictionary<Type, HashSet<Delegate>> _typedHandlers = new ();
        
        //////////////////////////////////////////////////////////////////////////
        /// IService
        public void Reset()
        {
            _typedHandlers.Clear();
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
            
            if (_typedHandlers.TryGetValue(eventType, out var handlerList))
            {
                foreach (var handler in handlerList)
                {
                    ((Action<T>)handler)(evt);
                }
            }
        }
        

        public void AddHandler<T>(Action<T> handler) where T : struct, IValueEvent
        {
            Type type = typeof(T);
            
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
