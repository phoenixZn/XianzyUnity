using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Xease;

namespace Xease.CoreGame
{
    public interface IVariables
    {
        System.Type VarType { get; }
        bool HasVar(string id);
        bool ClearVar(string key);
        void Clear();
        void CopyTo(VarEnv env, bool skipSameKey = true, bool logSameKey = true);
        void ForeachCollect(Action<string, object> onCollect);  // 非必要慎用
    }

    /// <summary>
    /// 友元标记接口：持有该凭证的对象可直接读取 VarEnv / VariablesImp 内部字典
    /// </summary>
    public interface IVarEnvFriend
    {
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 单一值类型的变量桶：string key → T。
    /// </summary>
    public class VariablesImp<T> : IVariables
    {
        // 桶内变量表；容量按常用规模预留
        protected Dictionary<string, T> _varDic = new (4);

        /// <summary>分桶值类型。</summary>
        public System.Type VarType => typeof(T);

        /// <summary>
        /// 供 IVarEnvFriend 友元获取桶内字典；无效时返回 null。
        /// </summary>
        public Dictionary<string, T> GetRawDictionary(IVarEnvFriend vfriend)
        {
            if (vfriend != null)
                return _varDic;
            return null;
        }

        public void WriteVar(string id, T value)
        {
            _varDic[id] = value;
        }

        public bool ReadVar(string id, out T getV)
        {
            if (_varDic.TryGetValue(id, out getV))
            {
                return true;
            }

            getV = default(T);
            return false;
        }

        public bool HasVar(string id)
        {
            return _varDic.ContainsKey(id);
        }

        public bool ClearVar(string key)
        {
            return _varDic.Remove(key);
        }

        public void Clear()
        {
            _varDic.Clear();
        }

        public void CopyTo(VarEnv env, bool skipSameKey = true, bool logSameKey = true)
        {
            foreach (var kv in _varDic)
            {
                var key = kv.Key;
                var value = kv.Value;
                if (env.HasVar<T>(key))
                {
                    if (logSameKey)
                    {
                        env.ReadVar<T>(key, out var oldV);
                    }

                    if (skipSameKey)
                    {
                        continue;
                    }
                }

                env.WriteVar(key, value);
            }
        }

        public void ForeachCollect(Action<string, object> onCollect)
        {
            foreach (var kv in _varDic)
            {
                onCollect(kv.Key, kv.Value);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 变量黑板：int/bool/float/long/object 走定长热桶，其余类型用稠密 TypeKey 字典。
    /// </summary>
    public partial class VarEnv : ICanRecycle
    {
        // 热类型槽表：下标即 FastSlot；增删只改此处，桶长与 Bind 同源
        private static readonly System.Type[] s_fastTypes =
        {
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(double),
            typeof(long),
            typeof(object),
        };

        static VarEnv()
        {
            TypeKey<VarEnv>.Bind(s_fastTypes);
        }

        // 热桶 + 冷分类器；编号空间 TypeKey<VarEnv>
        private TypeKey<VarEnv>.Store<IVariables> _store = new TypeKey<VarEnv>.Store<IVariables>(4);

        // 按T缓存的手工定制读写路由：精确值类型桶 / object 桶 / uint→int
        private const byte RouteExact = 0;
        private const byte RouteObject = 1;
        private const byte RouteUIntAsInt = 2;  //篡改unit到int

        private static class TypeRoute<T>
        {
            // 按 T 只初始化一次：Exact / Object / UIntAsInt
            public static readonly byte Route = ResolveRoute();

            private static byte ResolveRoute()
            {
                var t = typeof(T);
                // class / interface（等价于原 IsClass||IsInterface）
                if (!t.IsValueType)
                    return RouteObject;
                if (t == typeof(uint))
                    return RouteUIntAsInt;
                return RouteExact;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// ICanRecycle:
        public bool IsInPool { get; private set; } = false;

        public VarEnv()
        {
        }
        
        public void Construct()
        {
            IsInPool = false;
        }

        public void Destroy()
        {
            IsInPool = true;
            Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        /// <summary>
        /// 预创建 s_fastTypes 对应的 IVariables 热桶；Clear/Destroy 只清字典、桶实例保留。
        /// 须与 s_fastTypes 保持同步。
        /// </summary>
        public void WarmupFastBuckets()
        {
            GetVariables<int>(true);
            GetVariables<bool>(true);
            GetVariables<float>(true);
            GetVariables<double>(true);
            GetVariables<long>(true);
            GetVariables<object>(true);

#if UNITY_EDITOR
            int warmed = 0;
            var buckets = _store.FastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                if (buckets[i] != null)
                {
                    warmed++;
                }
            }
            CLogger.LogAssert(warmed == TypeKey<VarEnv>.FastCount,
                $"VarEnv.WarmupFastBuckets 与 s_fastTypes 不同步: warmed={warmed}, expected={TypeKey<VarEnv>.FastCount}");
#endif
        }

        private VariablesImp<T> GetVariables<T>(bool autoAdd = false)
        {
            var vars = _store.Get<T>();
            if (vars != null)
            {
                return vars as VariablesImp<T>;
            }
            if (!autoAdd)
            {
                return null;
            }

            var variables = new VariablesImp<T>();
            _store.Set<T>(variables);
            return variables;
        }
        

        public bool ReadVar<T>(string key, out T value)
        {
            switch (TypeRoute<T>.Route)
            {
                case RouteObject:
                {
                    var variables = GetVariables<object>();
                    if (variables != null && variables.ReadVar(key, out var exist) && exist is T v)
                    {
                        value = v;
                        return true;
                    }
                    break;
                }
                case RouteUIntAsInt:
                {
                    var variables = GetVariables<int>();
                    if (variables != null && variables.ReadVar(key, out var exist))
                    {
                        value = Unsafe.As<int, T>(ref exist);
                        return true;
                    }
                    break;
                }
                default:
                {
                    var variables = GetVariables<T>();
                    if (variables != null && variables.ReadVar(key, out value))
                    {
                        return true;
                    }
                    break;
                }
            }

            value = default;
            return false;
        }

        public void WriteVar<T>(string key, T value)
        {
            switch (TypeRoute<T>.Route)
            {
                case RouteObject:
                    GetVariables<object>(true).WriteVar(key, value);
                    return;
                case RouteUIntAsInt:
                {
                    var intV = Unsafe.As<T, int>(ref value);
                    GetVariables<int>(true).WriteVar(key, intV);
                    return;
                }
                default:
                    GetVariables<T>(true).WriteVar(key, value);
                    return;
            }
        }
        
        //////////////////////////////////////////////////////////////////////////
        ///IVariables 相关操作 
        public bool HasVar<T>(string key)
        {
            switch (TypeRoute<T>.Route)
            {
                case RouteObject:
                    return GetVariables<object>()?.HasVar(key) ?? false;
                case RouteUIntAsInt:
                    return GetVariables<int>()?.HasVar(key) ?? false;
                default:
                    return GetVariables<T>()?.HasVar(key) ?? false;
            }
        }
        public bool ClearVar<T>(string key)
        {
            switch (TypeRoute<T>.Route)
            {
                case RouteObject:
                    return GetVariables<object>()?.ClearVar(key) ?? false;
                case RouteUIntAsInt:
                    return GetVariables<int>()?.ClearVar(key) ?? false;
                default:
                    return GetVariables<T>()?.ClearVar(key) ?? false;
            }
        }
        
        public void Clear()
        {
            var buckets = _store.FastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i]?.Clear();
            }

            var classifier = _store.Classifier;
            if (classifier != null)
            {
                foreach (var kv in classifier)
                {
                    kv.Value.Clear();
                }
            }
        }

        public void CopyTo(in VarEnv env)
        {
            var buckets = _store.FastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i]?.CopyTo(env);
            }

            var classifier = _store.Classifier;
            if (classifier == null)
            {
                return;
            }

            foreach (var item in classifier)
            {
                item.Value.CopyTo(env);
            }
        }

        public bool CopyTo<T>(in VarEnv env, string key, bool logError = true)
        {
            if (env.HasVar<T>(key))
                return false;
            if (ReadVar<T>(key, out var value))
            {
                env.WriteVar<T>(key, value);
                return true;
            }
            if (logError)
            {
                CLogger.LogError($"复制黑板时，没有找到变量! key={key}, valueType={typeof(T)}");
            }
            return false;
        }

        public bool CopyTo<T>(in VarEnv env, string key, string newKey)
        {
            if (env.HasVar<T>(newKey))
                return false;
            if (ReadVar<T>(key, out var value))
            {
                env.WriteVar<T>(newKey, value);
                return true;
            }
            CLogger.LogError($"复制黑板时，没有找到变量! key={key}, newKey={newKey}, valueType={typeof(T)}");
            return false;
        }
        
        /// <summary>
        /// 供 IVarEnvFriend 友元遍历类型分桶；凭证无效时不回调。
        /// </summary>
        public void ForeachBuckets(IVarEnvFriend vfriend, Action<System.Type, IVariables> onBucket)
        {
            if (vfriend == null || onBucket == null)
            {
                return;
            }

            var buckets = _store.FastBuckets;
            var fastTypes = TypeKey<VarEnv>.FastTypes;
            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket != null)
                {
                    onBucket(fastTypes[i], bucket);
                }
            }

            var classifier = _store.Classifier;
            if (classifier == null)
            {
                return;
            }

            foreach (var kv in classifier)
            {
                var bucket = kv.Value;
                if (bucket != null)
                {
                    onBucket(bucket.VarType, bucket);
                }
            }
        }
    }
}
