using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Xease
{
    /// <summary>
    /// 按宿主类型 TSpace 隔离的稠密 TypeKey：热类型走 FastSlot 数组下标，冷类型走单调 int Id。
    /// 编号只经 Of&lt;T&gt; 静态字段解析一次，禁止 Dictionary&lt;Type, _&gt;，避免 Type 哈希。
    /// Bind 仅在 TSpace 静态构造中调用一次；无热类型可不 Bind（全走 classifier）。
    /// 实例桶见嵌套 Store。
    /// </summary>
    public static class TypeKey<TSpace>
    {
        // 热类型槽表；未 Bind 时为空，ResolveFastSlot 恒为 -1
        private static Type[] s_fastTypes = Array.Empty<Type>();
        // 下一冷类型 Id；Bind 后从 FastCount 起跳，与热槽下标不重叠
        private static int s_nextTypeKey;
        // Bind 只生效一次，避免 Of<T> 已缓存 FastSlot 后改表
        private static bool s_bound;

        static TypeKey()
        {
            // 先跑宿主静态构造，确保 Bind 早于首次 Of<T>
            RuntimeHelpers.RunClassConstructor(typeof(TSpace).TypeHandle);
        }

        //////////////////////////////////////////////////////////////////////////
        /// TypeKey: static

        /// <summary>
        /// 绑定热类型表；下标即 FastSlot。应在 TSpace 静态构造中调用一次；重复调用忽略。
        /// </summary>
        public static void Bind(Type[] fastTypes)
        {
            if (s_bound)
            {
                return;
            }

            if (fastTypes == null || fastTypes.Length == 0)
            {
                s_fastTypes = Array.Empty<Type>();
                s_bound = true;
                return;
            }

            var copy = new Type[fastTypes.Length];
            Array.Copy(fastTypes, copy, fastTypes.Length);
            s_fastTypes = copy;
            if (s_nextTypeKey < copy.Length)
            {
                s_nextTypeKey = copy.Length;
            }

            s_bound = true;
        }

        /// <summary>热槽数量；与 Store.FastBuckets.Length 同源。</summary>
        public static int FastCount => s_fastTypes.Length;

        /// <summary>已绑定热类型表（Bind 时拷贝）；未 Bind 时为空数组。</summary>
        public static Type[] FastTypes => s_fastTypes;

        /// <summary>
        /// 按 T 缓存的 FastSlot 与冷 Id；静态只解析一次，热路径只读这两个 int。
        /// </summary>
        public static class Of<T>
        {
            // >=0 热桶下标；-1 走 classifier
            public static readonly int FastSlot = ResolveFastSlot(typeof(T));
            // 仅 FastSlot < 0 时分配；热类型为 -1。Interlocked 分配，不经 Type 哈希
            public static readonly int Id = FastSlot >= 0
                ? -1
                : Interlocked.Increment(ref s_nextTypeKey) - 1;
        }

        // 线性扫热表（引用相等，非 Type.GetHashCode）；仅 Of<T> 静态初始化调用一次
        private static int ResolveFastSlot(Type type)
        {
            var fastTypes = s_fastTypes;
            for (int i = 0; i < fastTypes.Length; i++)
            {
                if (type == fastTypes[i])
                {
                    return i;
                }
            }

            return -1;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>
        /// 实例侧热桶 + 冷分类器；作为宿主的 struct 字段持有，避免额外堆对象。
        /// 热路径 FastBuckets[FastSlot]；冷路径 Classifier 以 int Id 为键，不是 Type。
        /// TValue 按引用类型语义：Get 缺失时返回 null。须经由构造函数创建。
        /// </summary>
        public struct Store<TValue> where TValue : class
        {
            // 常用类型数组下标直达，绕过 Dictionary；长度 = FastCount
            public readonly TValue[] FastBuckets;
            // 非热类型分类器 <int TypeKey, TValue>；未写入冷类型时可为 null
            public Dictionary<int, TValue> Classifier;

            /// <summary>
            /// 按当前 FastCount 分配热桶。classifierCapacity &gt; 0 时预建分类器，否则懒创建。
            /// </summary>
            public Store(int classifierCapacity = 0)
            {
                FastBuckets = new TValue[TypeKey<TSpace>.FastCount];
                Classifier = classifierCapacity > 0
                    ? new Dictionary<int, TValue>(classifierCapacity)
                    : null;
            }

            /// <summary>按 T 取值；热槽走数组（含 null），冷槽走 int classifier，缺失返回 null。</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public TValue Get<T>()
            {
                var slot = Of<T>.FastSlot;
                if (slot >= 0)
                {
                    return FastBuckets[slot];
                }

                if (Classifier == null)
                {
                    return null;
                }

                Classifier.TryGetValue(Of<T>.Id, out var value);
                return value;
            }

            /// <summary>按 T 写入；热槽写下标，冷槽 Classifier.Add。调用方保证该类型尚未占用。</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Set<T>(TValue value)
            {
                var slot = Of<T>.FastSlot;
                if (slot >= 0)
                {
                    FastBuckets[slot] = value;
                    return;
                }

                Classifier ??= new Dictionary<int, TValue>();
                Classifier.Add(Of<T>.Id, value);
            }
        }
    }
}
