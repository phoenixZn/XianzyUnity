using System.Collections.Generic;
using UnityEngine;
using Xease.ModelPointTool.Gen;

namespace Xease.ModelPointTool
{
    /// <summary>
    /// 按预制体名与挂点名查询相对路径，并从实例根查找挂点 Transform。表由 <see cref="ModelPointGetterGen.RegisterAll"/> 填充。
    /// </summary>
    public static class ModelPointGetter
    {
        // (prefabName, pointName) → 相对预制体根的 Transform.Find 路径
        private static readonly Dictionary<(string, string), string> s_objNameWithPointPath = new();

        static ModelPointGetter()
        {
            ModelPointGetterGen.RegisterAll();
        }

        /// <summary>
        /// 按资源名查挂点相对路径；未登记时返回 null。
        /// </summary>
        public static string GetBindPointPath(string prefabName, string bindPoint)
        {
            if (s_objNameWithPointPath.TryGetValue((prefabName, bindPoint), out var path))
                return path;
            return null;
        }

        /// <summary>
        /// 从预制体实例根按登记路径查找挂点；root 为空、未登记或找不到时返回 null。
        /// </summary>
        public static Transform FindBindPoint(Transform prefabRoot, string prefabName, string bindPoint)
        {
            if (prefabRoot == null)
                return null;
            var path = GetBindPointPath(prefabName, bindPoint);
            if (string.IsNullOrEmpty(path))
                return null;
            return prefabRoot.Find(path);
        }

        /// <summary>
        /// 登记一条挂点路径；同键已存在时覆盖。仅生成配置调用。
        /// </summary>
        public static void Add(string prefabName, string bindPoint, string path)
        {
            s_objNameWithPointPath[(prefabName, bindPoint)] = path;
        }

        /// <summary>
        /// 清空挂点表。生成配置在重新填表前调用。
        /// </summary>
        public static void Clear()
        {
            s_objNameWithPointPath.Clear();
        }
    }
}
