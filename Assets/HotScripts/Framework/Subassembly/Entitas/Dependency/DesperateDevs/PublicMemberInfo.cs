using System;
using System.Collections.Generic;
using System.Reflection;

namespace DesperateDevs.Reflection
{
    /// <summary>
    /// 反射到的自定义特性及其公开成员，供 PublicMemberInfo 记录 attribute 元数据。
    /// </summary>
    public class AttributeInfo
    {
        // 特性实例
        public readonly object attribute;
        // 该特性类型上的公开字段/属性
        public readonly List<PublicMemberInfo> memberInfos;

        /// <summary>
        /// 绑定特性实例与其公开成员列表。
        /// </summary>
        public AttributeInfo(object attribute, List<PublicMemberInfo> memberInfos)
        {
            this.attribute = attribute;
            this.memberInfos = memberInfos;
        }
    }

    /// <summary>
    /// 公开实例字段或可读写属性的反射包装，供 CopyPublicMemberValues 读写。
    /// </summary>
    public class PublicMemberInfo
    {
        // 成员类型
        public readonly Type type;
        // 成员名
        public readonly string name;
        // 成员上的自定义特性
        public readonly AttributeInfo[] attributes;
        // 字段成员；与 _propertyInfo 互斥
        private readonly FieldInfo _fieldInfo;
        // 属性成员；与 _fieldInfo 互斥
        private readonly PropertyInfo _propertyInfo;

        /// <summary>
        /// 包装公开实例字段。
        /// </summary>
        public PublicMemberInfo(FieldInfo info)
        {
            this._fieldInfo = info;
            this.type = this._fieldInfo.FieldType;
            this.name = this._fieldInfo.Name;
            this.attributes = PublicMemberInfo.getAttributes(this._fieldInfo.GetCustomAttributes(false));
        }

        /// <summary>
        /// 包装可读写、无索引器的公开实例属性。
        /// </summary>
        public PublicMemberInfo(PropertyInfo info)
        {
            this._propertyInfo = info;
            this.type = this._propertyInfo.PropertyType;
            this.name = this._propertyInfo.Name;
            this.attributes = PublicMemberInfo.getAttributes(this._propertyInfo.GetCustomAttributes(false));
        }

        /// <summary>
        /// 仅描述类型与名称，不绑定 FieldInfo/PropertyInfo。
        /// </summary>
        public PublicMemberInfo(Type type, string name, AttributeInfo[] attributes = null)
        {
            this.type = type;
            this.name = name;
            this.attributes = attributes;
        }

        /// <summary>
        /// 从 obj 读取该成员当前值。
        /// </summary>
        public object GetValue(object obj)
        {
            if (this._fieldInfo == null)
            {
                return this._propertyInfo.GetValue(obj, null);
            }
            return this._fieldInfo.GetValue(obj);
        }

        /// <summary>
        /// 把 value 写入 obj 的该成员。
        /// </summary>
        public void SetValue(object obj, object value)
        {
            if (this._fieldInfo != null)
            {
                this._fieldInfo.SetValue(obj, value);
                return;
            }
            this._propertyInfo.SetValue(obj, value, null);
        }

        // 把 GetCustomAttributes 结果转成 AttributeInfo，并递归收集特性类型的公开成员
        private static AttributeInfo[] getAttributes(object[] attributes)
        {
            AttributeInfo[] array = new AttributeInfo[attributes.Length];
            for (int i = 0; i < attributes.Length; i++)
            {
                object obj = attributes[i];
                array[i] = new AttributeInfo(obj, obj.GetType().GetPublicMemberInfos());
            }
            return array;
        }
    }
}
