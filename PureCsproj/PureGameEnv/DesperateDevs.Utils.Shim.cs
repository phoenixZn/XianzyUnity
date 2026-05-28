using System;
using System.Collections.Generic;
using System.Reflection;

namespace DesperateDevs.Utils
{
    /// <summary>
    /// Minimal stand-in for Entitas-bundled DesperateDevs.Utils (not shipped as sources in this repo).
    /// Lives only in PureGameEnv; behavior matches constructor usage in Entitas Context.
    /// </summary>
    public sealed class ObjectPool<T>
    {
        readonly Func<T> _factory;
        readonly Action<T> _reset;
        readonly Stack<T> _pool = new Stack<T>();

        public ObjectPool(Func<T> factory, Action<T> reset)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _reset = reset;
        }

        public T Get()
        {
            return _pool.Count > 0 ? _pool.Pop() : _factory();
        }

        public void Push(T obj)
        {
            _reset?.Invoke(obj);
            _pool.Push(obj);
        }
    }

    /// <summary>
    /// Extension used by Entitas PublicMemberInfoEntityExtension (component cloning).
    /// </summary>
    public static class PublicMemberInfoCopyExtension
    {
        public static void CopyPublicMemberValues(this object source, object target)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Type st = source.GetType();
            Type tt = target.GetType();

            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
            foreach (FieldInfo field in st.GetFields(flags))
            {
                FieldInfo other = tt.GetField(field.Name, flags);
                if (other != null && other.FieldType.IsAssignableFrom(field.FieldType))
                {
                    other.SetValue(target, field.GetValue(source));
                }
            }

            foreach (PropertyInfo prop in st.GetProperties(flags))
            {
                if (!prop.CanRead)
                {
                    continue;
                }

                PropertyInfo other = tt.GetProperty(prop.Name, flags);
                if (other != null && other.CanWrite && other.PropertyType.IsAssignableFrom(prop.PropertyType))
                {
                    other.SetValue(target, prop.GetValue(source));
                }
            }
        }
    }
}
