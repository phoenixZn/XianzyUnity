using System;
using MackySoft.XPool;

namespace Xease
{
    /// <summary>
    /// 共享对象池服务：热类型走定长桶，其余用稠密 TypeKey 字典，避免 Dictionary&lt;Type, object&gt;。
    /// 当前未 Bind 热类型，FastCount=0，全走 classifier。
    /// </summary>
    public class PoolService : IPoolService
    {
        // 热类型走定长桶，其余走 classifier；无 Bind 时 FastCount=0
        private TypeKey<PoolService>.Store<IPool> _store = new TypeKey<PoolService>.Store<IPool>(0);

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        public void Shutdown()
        {
            var buckets = _store.FastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i]?.Clear();
                buckets[i] = null;
            }

            var classifier = _store.Classifier;
            if (classifier == null)
            {
                return;
            }

            foreach (var pool in classifier.Values)
            {
                pool.Clear();
            }
            classifier.Clear();
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
            if (_store.Get<T>() != null)
            {
                G.LogError($"Register pool failed, already exists: {typeof(T).FullName}");
                return;
            }

            _store.Set<T>(new FactoryPool<T>(capacity, factory, onRent, onReturn, onRelease));
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
            _store.Get<T>()?.Clear();
        }

        public IPool<T> GetPool<T>() where T : class, new()
        {
            var stored = _store.Get<T>();
            if (stored != null)
            {
                return (IPool<T>)stored;
            }

            var pool = new FactoryPool<T>(IPoolService.DefaultCapacity, () => new T());
            _store.Set<T>(pool);
            return pool;
        }
    }
}
