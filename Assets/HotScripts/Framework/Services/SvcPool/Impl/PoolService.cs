using System;
using System.Collections.Generic;
using MackySoft.XPool;

namespace Xease
{
    /// <summary>
    /// 共享对象池服务：热类型走定长桶，其余用稠密 TypeKey 字典，避免 Dictionary&lt;Type, object&gt;。
    /// </summary>
    public class PoolService : IPoolService
    {
        // 常用类型数组下标直达，绕过 Dictionary；长度 = s_fastTypes.Length
        private readonly IPool[] _fastBuckets;
        // 非热类型分类器 <TypeKey, 池>
        private Dictionary<int, IPool> _classifier;

        //////////////////////////////////////////////////////////////////////////
        /// TypeKey: static
        // 热类型槽表：下标即 FastSlot；增删只改此处，桶长与 Resolve 同源
        private static readonly Type[] s_fastTypes = Array.Empty<Type>();
        // 进程内单调分配稠密 TypeKey
        private static int s_nextTypeKey = s_fastTypes.Length;
        private static class TypeKeyOf<T>
        {
            public static readonly int FastSlot = ResolveFastSlot();
            public static readonly int KeyId = FastSlot >= 0
                ? -1
                : System.Threading.Interlocked.Increment(ref s_nextTypeKey) - 1; // 仅 FastSlot < 0 时分配；
            private static int ResolveFastSlot()
            {
                var type = typeof(T);
                var fastTypes = s_fastTypes;
                for (int i = 0; i < fastTypes.Length; i++)
                {
                    if (type == fastTypes[i]) return i;
                }
                return -1;  // -1 = 非热类型
            }
        }

        public PoolService()
        {
            _fastBuckets = new IPool[s_fastTypes.Length];
            _classifier = new Dictionary<int, IPool>();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        public void Shutdown()
        {
            var buckets = _fastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i]?.Clear();
                buckets[i] = null;
            }

            var classifier = _classifier;
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
            if (TryGetStoredPool<T>() != null)
            {
                G.LogError($"Register pool failed, already exists: {typeof(T).FullName}");
                return;
            }

            StorePool<T>(new FactoryPool<T>(capacity, factory, onRent, onReturn, onRelease));
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
            TryGetStoredPool<T>()?.Clear();
        }

        public IPool<T> GetPool<T>() where T : class, new()
        {
            var stored = TryGetStoredPool<T>();
            if (stored != null)
            {
                return (IPool<T>)stored;
            }

            var pool = new FactoryPool<T>(IPoolService.DefaultCapacity, () => new T());
            StorePool<T>(pool);
            return pool;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        // 按 FastSlot / TypeKey 取已有池；缺失返回 null
        private IPool TryGetStoredPool<T>() where T : class, new()
        {
            var slot = TypeKeyOf<T>.FastSlot;
            if (slot >= 0)
            {
                return _fastBuckets[slot];
            }

            if (_classifier == null || !_classifier.TryGetValue(TypeKeyOf<T>.KeyId, out var pool))
            {
                return null;
            }
            return pool;
        }

        // 写入热桶或 classifier；调用方保证该类型尚未占用
        private void StorePool<T>(IPool pool) where T : class, new()
        {
            var slot = TypeKeyOf<T>.FastSlot;
            if (slot >= 0)
            {
                _fastBuckets[slot] = pool;
                return;
            }

            _classifier ??= new Dictionary<int, IPool>();
            _classifier.Add(TypeKeyOf<T>.KeyId, pool);
        }
    }
}
