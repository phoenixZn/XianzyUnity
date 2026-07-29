using System.Collections.Generic;

namespace Xease.CoreGame
{
    public sealed partial class AttributesComponent : LogicComponent
    {
        //属性分类器 <属性类别， 存放的属性表>
        protected Dictionary<System.Type, IAttributes> _classifier;

        public AttributesComponent()
        {
            _classifier = new Dictionary<System.Type, IAttributes>();
        }

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
            if (modifiers != null && modifiers.ContainsKey(attrName))
            {
                return modifiers[attrName];
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

        public TValue GetDefaultValue<TValue>(int attrName, TValue defaultValue = default, bool logError = true)
        {
            var modifiers = GetAttribute<TValue>(attrName, logError);
            if (modifiers != null)
            {
                return modifiers.DefaultValue;
            }

            return defaultValue;
        }

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

        public bool Has<TValue>(int attrName)
        {
            var modifier = GetAttribute<TValue>(attrName, false);
            return modifier != null;
        }

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

        public void ClearModify<TValue>(int attrName)
        {
            var modifier = GetAttribute<TValue>(attrName);
            if (modifier != null)
            {
                modifier.Clear();
            }
        }

        public void Clear()
        {
            _classifier.Clear();
        }

        private AttributeModifiers<TValue> getAttributesByType<TValue>(bool logError = false)
        {
            var type = typeof(TValue);
            if (!_classifier.ContainsKey(type))
            {
                if (logError)
                {
                    WLogger.LogError("getAttributesByType == null  " + typeof(TValue));
                }

                return null;
            }

            var dic = _classifier[type] as AttributeModifiers<TValue>;
            return dic;
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