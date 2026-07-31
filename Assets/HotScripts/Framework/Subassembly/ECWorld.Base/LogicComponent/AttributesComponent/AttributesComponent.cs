using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary>
    /// 实体属性组件：int/bool/double/float 走定长热桶，其余类型用稠密 TypeKey 字典。
    /// </summary>
    public sealed partial class AttributesComponent : LogicComponent
    {
        // 热类型槽表：下标即 FastSlot；增删只改此处，桶长与 Resolve 同源
        private static readonly System.Type[] s_fastTypes =
        {
            typeof(int),
            typeof(bool),
            typeof(double),
            typeof(float),
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

        /// <summary>按值类型桶写入属性；桶不存在则创建，同名已存在则失败。</summary>
        public bool SetAttribute<TValue>(int attrName, IModifyValue<TValue> multValue)
        {
            var modifiers = getAttributesByType<TValue>();
            if (modifiers == null)
            {
                modifiers = new AttributeModifiers<TValue>();
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

        /// <summary>读取属性默认值；缺失时返回 defaultValue。</summary>
        public TValue GetDefaultValue<TValue>(int attrName, TValue defaultValue = default, bool logError = true)
        {
            var modifiers = GetAttribute<TValue>(attrName, logError);
            if (modifiers != null)
            {
                return modifiers.DefaultValue;
            }

            return defaultValue;
        }

        /// <summary>尝试读取属性当前值；失败时写出 defaulValue。</summary>
        public bool TryGetValue<TValue>(int attrName, out TValue getValue, TValue defaulValue)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                getValue = modifier.Value;
                return true;
            }

            getValue = defaulValue;
            return false;
        }

        /// <summary>尝试读取属性当前值；成功时写回 getValue。</summary>
        public bool TryGetValue<TValue>(int attrName, ref TValue getValue)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                getValue = modifier.Value;
                return true;
            }

            return false;
        }

        /// <summary>对指定属性叠加一次修改（flag 标识来源）。</summary>
        public bool Modify<TValue>(int attrName, TValue getValue, int flag)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                modifier.AddChange(getValue, flag);
                return true;
            }

            return false;
        }

        /// <summary>是否存在指定属性修改器。</summary>
        public bool Has<TValue>(int attrName)
        {
            var modifier = GetAttribute<TValue>(attrName, false);
            return modifier != null;
        }

        /// <summary>移除指定 flag 的修改项。</summary>
        public bool RemoveModify<TValue>(int attrName, int flag)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                modifier.RemoveChange(flag);
                return true;
            }

            return false;
        }

        /// <summary>清空指定属性上的全部修改项。</summary>
        public void ClearModify<TValue>(int attrName)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                modifier.Clear();
            }
        }

        /// <summary>清空热桶与冷字典，供组件复用。</summary>
        public void Clear()
        {
            var buckets = _fastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i] = null;
            }
            _classifier?.Clear();
        }

        private AttributeModifiers<TValue> getAttributesByType<TValue>(bool logError = false)
        {
            var slot = TypeKeyOf<TValue>.FastSlot;
            IAttributes attrs;
            if (slot >= 0)
            {
                attrs = _fastBuckets[slot];
            }
            else if (_classifier == null || !_classifier.TryGetValue(TypeKeyOf<TValue>.Id, out attrs))
            {
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
    public partial class LogicEntity
    {
        public AttributesComponent comAttributes
        {
            get { return (AttributesComponent)GetComponent(LogicComponentsLookup.ComAttributes); }
        }

        public bool hasComAttributes
        {
            get { return HasComponent(LogicComponentsLookup.ComAttributes); }
        }

        public AttributesComponent AddComAttributes()
        {
            var index = LogicComponentsLookup.ComAttributes;
            var component = (AttributesComponent)CreateComponent(index, typeof(AttributesComponent));
            component.Clear();
            AddComponent(index, component);
            return component;
        }
    }
    

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComAttributesIndex = new(typeof(AttributesComponent));
        public static int ComAttributes => _ComAttributesIndex.Index;
    }
}