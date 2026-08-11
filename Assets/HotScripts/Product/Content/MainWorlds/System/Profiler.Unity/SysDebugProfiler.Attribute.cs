using System;
using System.Collections.Generic;
using Unity.Profiling;
using Xease.FP;

namespace Xease.CoreGame.Debug
{
    public partial class SysDebugProfiler
    {
        //////////////////////////////////////////////////////////////////////////
        /// Debug Action:

        private void InitAttributes()
        {
            InitAttributes_Old();
            InitAttributes_New();
            InitAttributes_NewFastKey();
            InitAttributes_SimpleArray();
            InitAttributes_SimpleDic();
        }

        public const int AttrKeyMin = 0;
        public const int AttrKeyMax = 4;
        public const int AttrKeyCount = AttrKeyMax + 1;
        public const int AttrImpListCount = 1000;

        //////////////////////////////////////////////////////////////////////////
        private static readonly ProfilerMarker s_attributeMarker = new("ComAttributes.Old");
        private List<DebugAttributesOld> attributesList_old = new(AttrImpListCount);
        private void InitAttributes_Old()
        {
            attributesList_old.Clear();
            for (int i = 0; i < AttrImpListCount; i++)
            {
                var attrs = new DebugAttributesOld();
                for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                {
                    attrs.SetAttribute(k, new MultChangeValue_Last<int>(k));
                    attrs.SetAttribute(k, new MultChangeValue_Last<double>(k));
                    attrs.SetAttribute(k, new MultChangeValue_Last<bool>(k % 2 == 0));
                    attrs.SetAttribute(k, new MultChangeValue_Last<float>(k));
                    attrs.SetAttribute(k, new MultChangeValue_Last<FixPoint>(new FixPoint(k)));
                }

                attributesList_old.Add(attrs);
            }
        }
        private void ProfilerAttributes_Old()
        {
            using (s_attributeMarker.Auto())
            {
                for (int i = 0; i < attributesList_old.Count; i++)
                {
                    var attrs = attributesList_old[i];
                    for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                    {
                        attrs.GetValue<int>(k, default, false);
                        attrs.GetValue<double>(k, default, false);
                        attrs.GetValue<bool>(k, default, false);
                        attrs.GetValue<float>(k, default, false);
                        attrs.GetValue<FixPoint>(k, default, false);
                    }
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////
        // ComAttributes.New = Dictionary 修改器表；ComAttributes.New.FastKey = 热键数组表
        private static readonly ProfilerMarker s_attributeMarkerNew = new("ComAttributes.New");
        private static readonly ProfilerMarker s_attributeMarkerNewFastKey = new("ComAttributes.New.FastKey");
        private List<DebugAttributesNew> attributesList_new = new(AttrImpListCount);
        private List<DebugAttributesNew> attributesList_newFastKey = new(AttrImpListCount);

        private void InitAttributes_New()
        {
            FillAttributesList_New(attributesList_new, useFastKeySlots: false);
        }

        private void InitAttributes_NewFastKey()
        {
            FillAttributesList_New(attributesList_newFastKey, useFastKeySlots: true);
        }

        private void FillAttributesList_New(List<DebugAttributesNew> list, bool useFastKeySlots)
        {
            list.Clear();
            for (int i = 0; i < AttrImpListCount; i++)
            {
                var attrs = new DebugAttributesNew(useFastKeySlots);
                for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                {
                    attrs.SetAttribute(k, new MultChangeValue_Last<int>(k));
                    attrs.SetAttribute(k, new MultChangeValue_Last<double>(k));
                    attrs.SetAttribute(k, new MultChangeValue_Last<bool>(k % 2 == 0));
                    attrs.SetAttribute(k, new MultChangeValue_Last<float>(k));
                    attrs.SetAttribute(k, new MultChangeValue_Last<FixPoint>(new FixPoint(k)));
                }

                list.Add(attrs);
            }
        }

        private void ProfilerAttributes_New()
        {
            ProfilerAttributes_NewCore(attributesList_new, s_attributeMarkerNew);
        }

        private void ProfilerAttributes_NewFastKey()
        {
            ProfilerAttributes_NewCore(attributesList_newFastKey, s_attributeMarkerNewFastKey);
        }

        private static void ProfilerAttributes_NewCore(List<DebugAttributesNew> list, ProfilerMarker marker)
        {
            using (marker.Auto())
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var attrs = list[i];
                    for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                    {
                        attrs.GetValue<int>(k, default, false);
                        attrs.GetValue<double>(k, default, false);
                        attrs.GetValue<bool>(k, default, false);
                        attrs.GetValue<float>(k, default, false);
                        attrs.GetValue<FixPoint>(k, default, false);
                    }
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////
        private static readonly ProfilerMarker s_attributeMarkerSimple = new("ComAttributes.SimpleArray");
        private List<DebugAttributesSimpleArray> attributesList_simple = new(AttrImpListCount);
        private void InitAttributes_SimpleArray()
        {
            attributesList_simple.Clear();
            for (int i = 0; i < AttrImpListCount; i++)
            {
                var attrs = new DebugAttributesSimpleArray();
                for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                {
                    attrs.IntAttrs[k] = new MultChangeValue_Last<int>(k);
                    attrs.DoubleAttrs[k] = new MultChangeValue_Last<double>(k);
                    attrs.BoolAttrs[k] = new MultChangeValue_Last<bool>(k % 2 == 0);
                    attrs.FloatAttrs[k] = new MultChangeValue_Last<float>(k);
                    attrs.FixPointAttrs[k] = new MultChangeValue_Last<FixPoint>(new FixPoint(k));
                }
                attributesList_simple.Add(attrs);
            }
        }
        private void ProfilerAttributes_SimpleArray()
        {
            using (s_attributeMarkerSimple.Auto())
            {
                for (int i = 0; i < attributesList_simple.Count; i++)
                {
                    var attrs = attributesList_simple[i];
                    for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                    {
                        attrs.GetInt(k);
                        attrs.GetDouble(k);
                        attrs.GetBool(k);
                        attrs.GetFloat(k);
                        attrs.GetFixPoint(k);
                    }
                }
            }
        }


        //////////////////////////////////////////////////////////////////////////
        private static readonly ProfilerMarker s_attributeMarkerSimpleDic = new("ComAttributes.SimpleDic");
        private List<DebugAttributesSimpleDic> attributesList_simpleDic = new(AttrImpListCount);
        private void InitAttributes_SimpleDic()
        {
            attributesList_simpleDic.Clear();
            for (int i = 0; i < AttrImpListCount; i++)
            {
                var attrs = new DebugAttributesSimpleDic();
                for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                {
                    attrs.IntAttrs.Add(k, new MultChangeValue_Last<int>(k));
                    attrs.DoubleAttrs.Add(k, new MultChangeValue_Last<double>(k));
                    attrs.BoolAttrs.Add(k, new MultChangeValue_Last<bool>(k % 2 == 0));
                    attrs.FloatAttrs.Add(k, new MultChangeValue_Last<float>(k));
                    attrs.FixPointAttrs.Add(k, new MultChangeValue_Last<FixPoint>(new FixPoint(k)));
                }
                attributesList_simpleDic.Add(attrs);
            }
        }
        private void ProfilerAttributes_SimpleDic()
        {
            using (s_attributeMarkerSimpleDic.Auto())
            {
                for (int i = 0; i < attributesList_simpleDic.Count; i++)
                {
                    var attrs = attributesList_simpleDic[i];
                    for (int k = AttrKeyMin; k <= AttrKeyMax; k++)
                    {
                        attrs.GetInt(k);
                        attrs.GetDouble(k);
                        attrs.GetBool(k);
                        attrs.GetFloat(k);
                        attrs.GetFixPoint(k);
                    }
                }
            }
        }
        
    }

    //////////////////////////////////////////////////////////////////////////
    public class DebugAttributesOld
    {
        //属性分类器 <属性类别， 存放的属性表>
        protected Dictionary<System.Type, IAttributes> _classifier = new();
        
        public bool SetAttribute<TValue>(int attrName, IModifyValue<TValue> multValue)
        {
            var modifiers = getAttributesByType<TValue>();
            if (modifiers == null)
            {
                modifiers = new AttributeModifiers<TValue>();
                _classifier.Add(typeof(TValue), modifiers);
            }
            else if (modifiers.ContainsKey(attrName))
            {
                return false;
            }

            modifiers.Add(attrName, multValue);
            return true;
        }
        
        public IModifyValue<TValue> GetAttribute<TValue>(int attrName, bool logError = true)
        {
            var modifiers = getAttributesByType<TValue>(logError);
            if (modifiers != null && modifiers.TryGetValue(attrName, out var modifier))
            {
                return modifier;
            }

            return null;
        }

        public TValue GetValue<TValue>(int attrName, TValue defaultValue = default, bool logError = true)
        {
            var modifiers = GetAttribute<TValue>(attrName, logError);
            if (modifiers != null)
            {
                return modifiers.Value;
            }
            return defaultValue;
        }
        

        public bool TryGetValue<TValue>(int attrName, out TValue getValue, TValue defaultValue)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                getValue = modifier.Value;
                return true;
            }

            getValue = defaultValue;
            return false;
        }

        private AttributeModifiers<TValue> getAttributesByType<TValue>(bool logError = false)
        {
            var type = typeof(TValue);
            if (!_classifier.TryGetValue(type, out var attrs))
            {
                if (logError)
                {
                    WLogger.LogError("getAttributesByType == null  " + typeof(TValue));
                }
                return null;
            }

            return attrs as AttributeModifiers<TValue>;
        }

        public interface IAttributes
        {
            System.Type AttributeType { get; }
        }
        public class AttributeModifiers<TValue> // TValue属性的数值类型
            : Dictionary<int, IModifyValue<TValue>> //<TEnum, 数值叠加计算器>
                , IAttributes
        {
            public System.Type AttributeType
            {
                get { return typeof(TValue); }
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 相对 Old：类型层 FastSlot + 修改器表双实现（Dictionary / 热键数组），由 UseFastKeySlots 在创建时选定。
    /// </summary>
    public class DebugAttributesNew
    {
        // true = 热键数组 AttributeModifiersFast；false = Dictionary AttributeModifiersDic
        public readonly bool UseFastKeySlots;

        // 热类型槽表：下标即 FastSlot；增删只改此处，桶长与 Resolve 同源
        private static readonly System.Type[] s_fastTypes =
        {
            typeof(int),
            typeof(bool),
            typeof(double),
            typeof(float),
            typeof(FixPoint),
        };

        // 常用类型下标直达，绕过 Dictionary；长度 = s_fastTypes.Length
        protected readonly IAttributes[] _fastBuckets = new IAttributes[s_fastTypes.Length];
        // 非热类型分类器 <TypeKey, 属性表>；首次写入懒创建
        protected Dictionary<int, IAttributes> _classifier;

        // 进程内仅非热类型单调分配稠密 TypeKey
        private static int s_nextTypeKey = s_fastTypes.Length;
        private static class TypeKeyOf<T>
        {
            // -1 = 非热类型，走 _classifier
            public static readonly int FastSlot = ResolveFastSlot();
            // 仅 FastSlot < 0 时分配；热类型 Id 无意义
            public static readonly int Id = FastSlot >= 0
                ? -1
                : System.Threading.Interlocked.Increment(ref s_nextTypeKey) - 1;

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
        }

        /// <summary>创建对照实例；useFastKeySlots 决定修改器表实现，创建后不可改。</summary>
        public DebugAttributesNew(bool useFastKeySlots = false)
        {
            UseFastKeySlots = useFastKeySlots;
        }

        /// <summary>按值类型桶写入属性；桶不存在则创建，同名已存在则失败。</summary>
        public bool SetAttribute<TValue>(int attrName, IModifyValue<TValue> multValue)
        {
            var modifiers = getAttributesByType<TValue>();
            if (modifiers == null)
            {
                modifiers = CreateModifiers<TValue>();
                var slot = TypeKeyOf<TValue>.FastSlot;
                if (slot >= 0)
                {
                    _fastBuckets[slot] = modifiers;
                }
                else
                {
                    _classifier ??= new Dictionary<int, IAttributes>();
                    _classifier.Add(TypeKeyOf<TValue>.Id, modifiers);
                }
            }
            else if (modifiers.ContainsKey(attrName))
            {
                return false;
            }

            modifiers.Add(attrName, multValue);
            return true;
        }

        /// <summary>按值类型桶与 attrName 取修改器；缺失时可打错误日志。</summary>
        public IModifyValue<TValue> GetAttribute<TValue>(int attrName, bool logError = true)
        {
            var modifiers = getAttributesByType<TValue>(logError);
            if (modifiers != null && modifiers.TryGetValue(attrName, out var modifier))
            {
                return modifier;
            }

            return null;
        }

        /// <summary>读取属性当前值；缺失时返回 defaultValue。</summary>
        public TValue GetValue<TValue>(int attrName, TValue defaultValue = default, bool logError = true)
        {
            var modifiers = GetAttribute<TValue>(attrName, logError);
            if (modifiers != null)
            {
                return modifiers.Value;
            }
            return defaultValue;
        }

        /// <summary>尝试读取属性当前值；失败时写出 defaultValue。</summary>
        public bool TryGetValue<TValue>(int attrName, out TValue getValue, TValue defaultValue)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                getValue = modifier.Value;
                return true;
            }

            getValue = defaultValue;
            return false;
        }

        private IAttributeModifiersTable<TValue> CreateModifiers<TValue>()
        {
            if (UseFastKeySlots)
            {
                return new AttributeModifiersFast<TValue>();
            }

            return new AttributeModifiersDic<TValue>();
        }

        private IAttributeModifiersTable<TValue> getAttributesByType<TValue>(bool logError = false)
        {
            var slot = TypeKeyOf<TValue>.FastSlot;
            IAttributes attrs;
            if (slot >= 0)
            {
                attrs = _fastBuckets[slot];
            }
            else
            {
                WLogger.LogError("getAttributesByType use _classifier :" + typeof(TValue));
                if (_classifier == null || !_classifier.TryGetValue(TypeKeyOf<TValue>.Id, out attrs))
                    attrs = null;
            }

            if (attrs == null)
            {
                if (logError)
                {
                    WLogger.LogError("getAttributesByType == null  " + typeof(TValue));
                }
                return null;
            }

            return attrs as IAttributeModifiersTable<TValue>;
        }

        public interface IAttributes
        {
            System.Type AttributeType { get; }
        }

        /// <summary>修改器表最小契约：Dic / Fast 共用，供类型桶读写。</summary>
        public interface IAttributeModifiersTable<TValue> : IAttributes
        {
            bool ContainsKey(int key);
            bool TryGetValue(int key, out IModifyValue<TValue> value);
            void Add(int key, IModifyValue<TValue> value);
        }

        /// <summary>老实现：整表 Dictionary&lt;attrName, 修改器&gt;。</summary>
        public class AttributeModifiersDic<TValue>
            : Dictionary<int, IModifyValue<TValue>>
            , IAttributeModifiersTable<TValue>
        {
            public System.Type AttributeType
            {
                get { return typeof(TValue); }
            }
        }

        /// <summary>
        /// 热键实现：key 在 [0, FastKeyCapacity) 走定长数组，其余走冷字典（对齐正式 AttributeModifiers）。
        /// </summary>
        public class AttributeModifiersFast<TValue> : IAttributeModifiersTable<TValue>
        {
            // 热 key 容量；与正式组件 / int 脏标记位宽对齐
            public const int FastKeyCapacity = 32;

            // 热槽懒分配；下标即 attrName，null 表示未占用
            private IModifyValue<TValue>[] _fastSlots;
            // 冷 key 字典；仅 key 越界时使用
            private Dictionary<int, IModifyValue<TValue>> _cold;

            //////////////////////////////////////////////////////////////////////////
            /// IAttributes:
            public System.Type AttributeType
            {
                get { return typeof(TValue); }
            }

            //////////////////////////////////////////////////////////////////////////
            /// This：
            /// <summary>是否已注册指定 attrName 的修改器。</summary>
            public bool ContainsKey(int key)
            {
                if ((uint)key < FastKeyCapacity)
                {
                    return _fastSlots != null && _fastSlots[key] != null;
                }

                return _cold != null && _cold.ContainsKey(key);
            }

            /// <summary>按 attrName 取修改器；未注册时返回 false。</summary>
            public bool TryGetValue(int key, out IModifyValue<TValue> value)
            {
                if ((uint)key < FastKeyCapacity)
                {
                    if (_fastSlots != null)
                    {
                        value = _fastSlots[key];
                        return value != null;
                    }

                    value = null;
                    return false;
                }

                if (_cold != null)
                {
                    return _cold.TryGetValue(key, out value);
                }

                value = null;
                return false;
            }

            /// <summary>注册修改器；同 key 已存在时抛 ArgumentException（对齐 Dictionary.Add）。</summary>
            public void Add(int key, IModifyValue<TValue> value)
            {
                if ((uint)key < FastKeyCapacity)
                {
                    _fastSlots ??= new IModifyValue<TValue>[FastKeyCapacity];
                    if (_fastSlots[key] != null)
                    {
                        throw new ArgumentException("An item with the same key has already been added.");
                    }

                    _fastSlots[key] = value;
                    return;
                }

                _cold ??= new Dictionary<int, IModifyValue<TValue>>();
                _cold.Add(key, value);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 无 Dictionary 的属性对照容器：按类型散放定长 MultChangeValue_Last 数组，key == index。
    /// </summary>
    public class DebugAttributesSimpleArray
    {
        private const int AttrKeyCount = SysDebugProfiler.AttrKeyCount;
        // key == index，长度
        public readonly MultChangeValue_Last<int>[] IntAttrs = new MultChangeValue_Last<int>[AttrKeyCount];
        public readonly MultChangeValue_Last<double>[] DoubleAttrs = new MultChangeValue_Last<double>[AttrKeyCount];
        public readonly MultChangeValue_Last<bool>[] BoolAttrs = new MultChangeValue_Last<bool>[AttrKeyCount];
        // float / FixPoint 对照槽，与 int 等同规模
        public readonly MultChangeValue_Last<float>[] FloatAttrs = new MultChangeValue_Last<float>[AttrKeyCount];
        public readonly MultChangeValue_Last<FixPoint>[] FixPointAttrs = new MultChangeValue_Last<FixPoint>[AttrKeyCount];

        /// <summary>按下标读取 int 属性当前值。</summary>
        public int GetInt(int key) => IntAttrs[key].Value;
        /// <summary>按下标读取 double 属性当前值。</summary>
        public double GetDouble(int key) => DoubleAttrs[key].Value;
        /// <summary>按下标读取 bool 属性当前值。</summary>
        public bool GetBool(int key) => BoolAttrs[key].Value;
        /// <summary>按下标读取 float 属性当前值。</summary>
        public float GetFloat(int key) => FloatAttrs[key].Value;
        /// <summary>按下标读取 FixPoint 属性当前值。</summary>
        public FixPoint GetFixPoint(int key) => FixPointAttrs[key].Value;
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 相对 Old 去掉 Type 分类层：按类型散放五个 Dictionary(int, IModifyValue)。
    /// </summary>
    public class DebugAttributesSimpleDic
    {
        private const int AttrKeyCount = SysDebugProfiler.AttrKeyCount;
        // 无 Type 分类层，直接按值类型分字典
        public readonly Dictionary<int, IModifyValue<int>> IntAttrs = new(AttrKeyCount);
        public readonly Dictionary<int, IModifyValue<double>> DoubleAttrs = new(AttrKeyCount);
        public readonly Dictionary<int, IModifyValue<bool>> BoolAttrs = new(AttrKeyCount);
        // float / FixPoint 对照字典，与 int 等同规模
        public readonly Dictionary<int, IModifyValue<float>> FloatAttrs = new(AttrKeyCount);
        public readonly Dictionary<int, IModifyValue<FixPoint>> FixPointAttrs = new(AttrKeyCount);

        /// <summary>按 key 读取 int 属性当前值。</summary>
        public int GetInt(int key) => IntAttrs.TryGetValue(key, out var v) ? v.Value : default;
        /// <summary>按 key 读取 double 属性当前值。</summary>
        public double GetDouble(int key) => DoubleAttrs.TryGetValue(key, out var v) ? v.Value : default;
        /// <summary>按 key 读取 bool 属性当前值。</summary>
        public bool GetBool(int key) => BoolAttrs.TryGetValue(key, out var v) ? v.Value : default;
        /// <summary>按 key 读取 float 属性当前值。</summary>
        public float GetFloat(int key) => FloatAttrs.TryGetValue(key, out var v) ? v.Value : default;
        /// <summary>按 key 读取 FixPoint 属性当前值。</summary>
        public FixPoint GetFixPoint(int key) => FixPointAttrs.TryGetValue(key, out var v) ? v.Value : default;
    }
}