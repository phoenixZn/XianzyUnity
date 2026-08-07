using System;
using System.Collections.Generic;
using MackySoft.XPool;

namespace Xease
{
    /// <summary>
    /// 共享对象池服务实现，按 Type 集中管理各类型池。
    /// </summary>
    public class PoolService : IPoolService
    {
        // 按类型存放 FactoryPool<T>（装箱为 object）
        private readonly Dictionary<Type, object> _pools = new();

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        public void Shutdown()
        {
            foreach (var poolObj in _pools.Values)
            {
                ((IPool)poolObj).Clear();
            }
            _pools.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IPoolService:
        public void Register<T>(
            Func<T> factory,
            int capacity = IPoolService.DefaultCapacity,
            Action<T> onRent = null,
            Action<T> onReturn = null,
            Action<T> onRelease = null) where T : class, new()
        {
            Type type = typeof(T);
            if (_pools.ContainsKey(type))
            {
                G.LogError($"Register pool failed, already exists: {type.FullName}");
                return;
            }

            _pools[type] = new FactoryPool<T>(capacity, factory, onRent, onReturn, onRelease);
        }

        public T Rent<T>() where T : class, new()
        {
            return GetPool<T>().Rent();
        }

        public void Return<T>(T instance) where T : class, new()
        {
            if (instance == null)
            {
                G.LogError("Return pool instance is null");
                return;
            }

            GetPool<T>().Return(instance);
        }

        public void Prewarm<T>(int count) where T : class, new()
        {
            if (count <= 0)
            {
                return;
            }

            IPool<T> pool = GetPool<T>();
            T[] buffer = new T[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i] = pool.Rent();
            }
            for (int i = 0; i < count; i++)
            {
                pool.Return(buffer[i]);
            }
        }

        public void Clear<T>() where T : class, new()
        {
            Type type = typeof(T);
            if (!_pools.TryGetValue(type, out var poolObj))
            {
                return;
            }

            ((IPool)poolObj).Clear();
        }

        public IPool<T> GetPool<T>() where T : class, new()
        {
            Type type = typeof(T);
            if (_pools.TryGetValue(type, out var poolObj))
            {
                return (IPool<T>)poolObj;
            }

            var pool = new FactoryPool<T>(IPoolService.DefaultCapacity, () => new T());
            _pools[type] = pool;
            return pool;
        }
    }
}
