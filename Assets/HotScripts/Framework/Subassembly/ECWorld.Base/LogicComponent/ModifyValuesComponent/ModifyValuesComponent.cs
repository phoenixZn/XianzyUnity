namespace Xease.CoreGame
{
    public interface IModifyValuesContainer
    {
        System.Type AttributeType { get; }
        // 注销全部属性条目，保留底层数组/字典容量
        void Clear();
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 万能冷属性状态组件：用于存放极特殊的、不常见的、可自定义修改规则的状态数据
    /// int/bool/double/float 走定长热桶，其余类型用稠密 TypeKey 字典。
    /// </summary>
    public sealed class ModifyValuesComponent : LogicComponent
    {
        // 热类型槽表：下标即 FastSlot；增删只改此处，桶长与 Bind 同源
        private static readonly System.Type[] s_fastTypes =
        {
            typeof(int),
            typeof(bool),
            typeof(double),
            typeof(float),
        };

        static ModifyValuesComponent()
        {
            TypeKey<ModifyValuesComponent>.Bind(s_fastTypes);
        }
        // 热桶 + 冷分类器；编号空间
        private TypeKey<ModifyValuesComponent>.Store<IModifyValuesContainer> _store = new (0);
        
        
        /// <summary>按值类型桶写入冷属性状态；桶不存在则创建，同名已存在则失败。</summary>
        public bool SetModifyValue<TValue>(int attrName, IModifyValue<TValue> multValue)
        {
            if (multValue == null)
            {
                WLogger.LogError($"SetAttribute  multValue == null， attrName={attrName}");
                return false;
            }
            var modifiers = getContainerByType<TValue>();
            if (modifiers == null)
            {
                modifiers = new ModifyValuesContainer<TValue>();
                _store.Set<TValue>(modifiers);
            }
            else if (modifiers.ContainsKey(attrName))
            {
                return false;
            }

            modifiers.Add(attrName, multValue);
            return true;
        }

        /// <summary>按值类型桶与 attrName 取修改器；缺失时可打错误日志。</summary>
        public IModifyValue<TValue> GetModifyValue<TValue>(int attrName, bool logError = true)
        {
            var modifiers = getContainerByType<TValue>(logError);
            if (modifiers != null && modifiers.TryGetValue(attrName, out var modifier))
            {
                return modifier;
            }
            return null;
        }

        /// <summary>读取冷属性当前值；缺失时返回 defaultValue。</summary>
        public TValue GetValue<TValue>(int attrName, TValue defaultValue = default, bool logError = true)
        {
            var modifiers = GetModifyValue<TValue>(attrName, logError);
            if (modifiers != null)
            {
                return modifiers.Value;
            }
            return defaultValue;
        }

        /// <summary>读取冷属性默认值；缺失时返回 defaultValue。</summary>
        public TValue GetDefaultValue<TValue>(int attrName, TValue defaultValue = default, bool logError = true)
        {
            var modifiers = GetModifyValue<TValue>(attrName, logError);
            if (modifiers != null)
            {
                return modifiers.DefaultValue;
            }
            return defaultValue;
        }

        /// <summary>尝试读取冷属性当前值；失败时写出 defaultValue。</summary>
        public bool TryGetValue<TValue>(int attrName, out TValue getValue, TValue defaultValue)
        {
            var modifier = GetModifyValue<TValue>(attrName);
            if (modifier != null)
            {
                getValue = modifier.Value;
                return true;
            }
            getValue = defaultValue;
            return false;
        }

        /// <summary>尝试读取冷属性当前值；成功时写回 getValue。</summary>
        public bool TryGetValue<TValue>(int attrName, ref TValue getValue)
        {
            var modifier = GetModifyValue<TValue>(attrName);
            if (modifier != null)
            {
                getValue = modifier.Value;
                return true;
            }
            return false;
        }

        /// <summary>对指定冷属性叠加一次修改（flag 标识来源）。</summary>
        public bool Modify<TValue>(int attrName, TValue getValue, int flag)
        {
            var modifier = GetModifyValue<TValue>(attrName);
            if (modifier != null)
            {
                modifier.AddChange(getValue, flag);
                return true;
            }
            return false;
        }

        /// <summary>是否存在指定冷属性修改器。</summary>
        public bool Has<TValue>(int attrName)
        {
            var modifier = GetModifyValue<TValue>(attrName, false);
            return modifier != null;
        }

        /// <summary>移除指定 flag 的修改项。</summary>
        public bool RemoveModify<TValue>(int attrName, int flag)
        {
            var modifier = GetModifyValue<TValue>(attrName);
            if (modifier != null)
            {
                modifier.RemoveChange(flag);
                return true;
            }
            return false;
        }

        /// <summary>清空指定冷属性上的全部修改项。</summary>
        public void ClearModify<TValue>(int attrName)
        {
            var modifier = GetModifyValue<TValue>(attrName);
            if (modifier != null)
            {
                modifier.Clear();
            }
        }

        /// <summary>清空各类型冷属性表内容并保留子容器，供组件入池复用。</summary>
        public void Clear()
        {
            var buckets = _store.FastBuckets;
            for (int i = 0; i < buckets.Length; i++)
            {
                var attrs = buckets[i];
                attrs?.Clear();
            }

            var classifier = _store.Classifier;
            if (classifier != null)
            {
                foreach (var attrs in classifier.Values)
                {
                    attrs.Clear();
                }
            }
        }

        private ModifyValuesContainer<TValue> getContainerByType<TValue>(bool logError = false)
        {
            var attrs = _store.Get<TValue>();
            if (attrs == null)
            {
                if (logError)
                {
                    WLogger.LogError($"getAttributesByType == null  TValue={typeof(TValue)}");
                }
                return null;
            }

            return attrs as ModifyValuesContainer<TValue>;
        }
        
    }


    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public ModifyValuesComponent comModifyValues
        {
            get { return (ModifyValuesComponent)GetComponent(LogicComponentsLookup.ComModifyValues); }
        }

        public bool hasComModifyValues
        {
            get { return HasComponent(LogicComponentsLookup.ComModifyValues); }
        }

        public ModifyValuesComponent AddComModifyValues()
        {
            var index = LogicComponentsLookup.ComModifyValues;
            var component = (ModifyValuesComponent)CreateComponent(index, typeof(ModifyValuesComponent));
            component.Clear();
            AddComponent(index, component);
            return component;
        }
    }
    

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComModifyValuesIndex = new(typeof(ModifyValuesComponent));
        public static int ComModifyValues => _ComModifyValuesIndex.Index;
    }
}