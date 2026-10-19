using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xease
{
    /// <summary>
    /// 白名单：标注后的具体模块会在环境创建时自动实例化并注册。
    /// 未标注的 IModule 实现不进入自动注册。Unity 与命令行宿主共用收集逻辑。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ModuleAutoRegisterAttribute : Attribute
    {
    }

    /// <summary>
    /// 按 <see cref="ModuleAutoRegisterAttribute"/> 从程序集收集可实例化的模块类型。
    /// 引用了当前宿主未加载程序集的类型会被跳过。
    /// </summary>
    public static class ModuleAutoRegister
    {
        /// <summary>
        /// 收集 <paramref name="assembly"/> 中应自动注册的模块类型。
        /// </summary>
        public static IEnumerable<Type> Collect(Assembly assembly)
        {
            return GetLoadableTypes(assembly).Where(IsAutoRegisterModule);
        }

        static bool IsAutoRegisterModule(Type type)
        {
            if (type == null || type.GetCustomAttribute<ModuleAutoRegisterAttribute>(false) == null)
                return false;

            if (type.IsAbstract || type.IsInterface || !typeof(IModule).IsAssignableFrom(type))
            {
                G.LogError($"[ModuleAutoRegister] {type} 标注了 ModuleAutoRegisterAttribute，但不是可实例化的 IModule");
                return false;
            }

            return true;
        }

        // 部分类型引用了当前宿主未加载的程序集，GetTypes 会抛 ReflectionTypeLoadException
        static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
