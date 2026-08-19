using System;
using System.Collections.Generic;
using System.Reflection;

namespace DesperateDevs.Reflection
{
    /// <summary>
    /// 公开成员克隆/拷贝扩展。Entitas CopyTo 使用 CopyPublicMemberValues。
    /// </summary>
    public static class PublicMemberInfoExtension
    {
        /// <summary>
        /// 收集 type 上公开实例字段，以及可读写、无索引器的公开实例属性。
        /// </summary>
        public static List<PublicMemberInfo> GetPublicMemberInfos(this Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
            List<PublicMemberInfo> list = new List<PublicMemberInfo>(fields.Length + properties.Length);
            for (int i = 0; i < fields.Length; i++)
            {
                list.Add(new PublicMemberInfo(fields[i]));
            }
            for (int j = 0; j < properties.Length; j++)
            {
                PropertyInfo propertyInfo = properties[j];
                if (propertyInfo.CanRead && propertyInfo.CanWrite && propertyInfo.GetIndexParameters().Length == 0)
                {
                    list.Add(new PublicMemberInfo(propertyInfo));
                }
            }
            return list;
        }

        /// <summary>
        /// 按公开成员浅拷贝出一个同类型新实例。
        /// </summary>
        public static object PublicMemberClone(this object obj)
        {
            object obj2 = Activator.CreateInstance(obj.GetType());
            obj.CopyPublicMemberValues(obj2);
            return obj2;
        }

        /// <summary>
        /// 按公开成员浅拷贝到新的 T 实例。
        /// </summary>
        public static T PublicMemberClone<T>(this object obj) where T : new()
        {
            T t = Activator.CreateInstance<T>();
            obj.CopyPublicMemberValues(t);
            return t;
        }

        /// <summary>
        /// 把 source 的公开字段/可写属性值写到 target（Entitas 组件克隆用）。
        /// </summary>
        public static void CopyPublicMemberValues(this object source, object target)
        {
            List<PublicMemberInfo> publicMemberInfos = source.GetType().GetPublicMemberInfos();
            for (int i = 0; i < publicMemberInfos.Count; i++)
            {
                PublicMemberInfo publicMemberInfo = publicMemberInfos[i];
                publicMemberInfo.SetValue(target, publicMemberInfo.GetValue(source));
            }
        }
    }
}
