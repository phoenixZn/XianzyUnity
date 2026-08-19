using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
        // 热类型槽表：下标即 FastSlot；增删只改此处，桶长与 Resolve 同源
        private static readonly System.Type[] s_fastTypes =
        {
            typeof(int),
            typeof(bool),
            typeof(float),
            typeof(double),
            typeof(long),
            typeof(object),
        };

        // 常用类型下标直达，绕过 Dictionary；
        private readonly IVariables[] _fastBuckets = new IVariables[s_fastTypes.Length];
        // 非热类型分类器 <TypeKey, 变量桶>；
        private Dictionary<int, IVariables> _varTypeDic = new (4);

        // 进程内仅非热类型单调分配稠密 TypeKey
        private static int s_nextTypeKey = s_fastTypes.Length;

        // 按T缓存的手工定制读写路由：精确值类型桶 / object 桶 / uint→int
        private const byte RouteExact = 0;
        private const byte RouteObject = 1;
        private const byte RouteUIntAsInt = 2;  //篡改unit到int

        private static class TypeKeyOf<T>
        {
            // -1 = 非热类型，走 _varTypeDic
            public static readonly int FastSlot = ResolveFastSlot();
            // 仅 FastSlot < 0 时分配；热类型 Id 无意义
            public static readonly int Id = FastSlot >= 0
                ? -1
                : System.Threading.Interlocked.Increment(ref s_nextTypeKey) - 1;
            // 按 T 只初始化一次：Exact / Object / UIntAsInt
            public static readonly byte Route = ResolveRoute();

            private static int ResolveFastSlot()
            {
                var type = typeof(T);
                var fastTypes = s_fastTypes;
                for (int i = 0; i < fastTypes.Length; i++)
                {
                    if (type == fastTypes[i]) return i;
                }
                return -1;
            }

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
        private VariablesImp<T> GetVariables<T>(bool autoAdd = false)
        {
            var slot = TypeKeyOf<T>.FastSlot;
            IVariables vars;
            if (slot >= 0)
            {
                vars = _fastBuckets[slot];
            }
            else if (_varTypeDic == null || !_varTypeDic.TryGetValue(TypeKeyOf<T>.Id, out vars))
            {
                vars = null;
            }
            if (vars != null)
            {
                return vars as VariablesImp<T>;
            }
            if (!autoAdd)
            {
                return null;
            }

            var variables = new VariablesImp<T>();
            StoreVariables(variables);
            return variables;
        }

        private void StoreVariables<T>(VariablesImp<T> variables)
        {
            var slot = TypeKeyOf<T>.FastSlot;
            if (slot >= 0)
            {
                _fastBuckets[slot] = variables;
                return;
            }
            _varTypeDic ??= new Dictionary<int, IVariables>();
            _varTypeDic.Add(TypeKeyOf<T>.Id, variables);
        }
        

        public bool ReadVar<T>(string key, out T value)
        {
            switch (TypeKeyOf<T>.Route)
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
            switch (TypeKeyOf<T>.Route)
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
            switch (TypeKeyOf<T>.Route)
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
            switch (TypeKeyOf<T>.Route)
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
            var buckets = _fastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i]?.Clear();
            }

            if (_varTypeDic != null)
            {
                foreach (var kv in _varTypeDic)
                {
                    kv.Value.Clear();
                }
            }
        }

        public void CopyTo(in VarEnv env)
        {
            var buckets = _fastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i]?.CopyTo(env);
            }

            if (_varTypeDic == null)
            {
                return;
            }

            foreach (var item in _varTypeDic)
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

            var buckets = _fastBuckets;
            var fastTypes = s_fastTypes;
            for (int i = 0; i < buckets.Length; i++)
            {
                var bucket = buckets[i];
                if (bucket != null)
                {
                    onBucket(fastTypes[i], bucket);
                }
            }

            if (_varTypeDic == null)
            {
                return;
            }

            foreach (var kv in _varTypeDic)
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
