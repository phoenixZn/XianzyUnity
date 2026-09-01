using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModelPointTool.Editor
{
    /// <summary>
    /// 模型挂点生成配置：多个模型根目录与要扫描的挂点名。仅编辑器使用。
    /// </summary>
    [CreateAssetMenu(fileName = "ModelPointSettings", menuName = "ModelPointTool/Create Model Point Settings")]
    public class ModelPointSettings : ScriptableObject
    {
        [Header("挂点名称列表")]
        // 按节点名在 Prefab 层级中匹配的挂点
        public string[] BindPointNameList =
        {
            "Hit", "Hp"
        };

        [Header("模型根目录")]
        [Tooltip("直接拖拽文件夹到这里，可配置多个")]
        // 扫描根列表，每项必须是文件夹
        public DefaultAsset[] ModelRootPaths;

        /// <summary>
        /// 当前有效模型根的 Asset 路径；空引用与非文件夹项会被跳过，路径去重。
        /// </summary>
        public string[] GetValidFolderPaths()
        {
            if (ModelRootPaths == null || ModelRootPaths.Length == 0)
                return System.Array.Empty<string>();

            var list = new List<string>();
            for (var i = 0; i < ModelRootPaths.Length; i++)
            {
                var root = ModelRootPaths[i];
                if (root == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(root);
                if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
                    continue;
                if (!list.Contains(path))
                    list.Add(path);
            }

            return list.ToArray();
        }

        void OnValidate()
        {
            if (ModelRootPaths == null)
                return;

            for (var i = 0; i < ModelRootPaths.Length; i++)
            {
                var root = ModelRootPaths[i];
                if (root == null)
                    continue;

                var path = AssetDatabase.GetAssetPath(root);
                if (AssetDatabase.IsValidFolder(path))
                    continue;

                Debug.LogWarning("请选择文件夹而不是文件！");
                ModelRootPaths[i] = null;
            }
        }

        /// <summary>
        /// 收集全部模型根下 Prefab 的 Asset 路径，按路径去重。
        /// </summary>
        public string[] CollectPrefabPaths()
        {
            var folders = GetValidFolderPaths();
            if (folders.Length == 0)
                return System.Array.Empty<string>();

            return CollectPrefabPathsInFolders(folders);
        }

        /// <summary>
        /// 收集指定文件夹下全部 Prefab 的 Asset 路径。
        /// </summary>
        public static string[] CollectPrefabPathsInFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                return System.Array.Empty<string>();

            return CollectPrefabPathsInFolders(new[] { folder });
        }

        /// <summary>
        /// 收集多个文件夹下全部 Prefab 的 Asset 路径，按路径去重。
        /// </summary>
        public static string[] CollectPrefabPathsInFolders(string[] folders)
        {
            if (folders == null || folders.Length == 0)
                return System.Array.Empty<string>();

            var validFolders = new List<string>();
            for (var i = 0; i < folders.Length; i++)
            {
                var folder = folders[i];
                if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                    continue;
                if (!validFolders.Contains(folder))
                    validFolders.Add(folder);
            }

            if (validFolders.Count == 0)
                return System.Array.Empty<string>();

            var list = new List<string>();
            var guids = AssetDatabase.FindAssets("t:Prefab", validFolders.ToArray());
            foreach (var guid in guids)
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath) || !File.Exists(assetPath))
                    continue;
                if (!list.Contains(assetPath))
                    list.Add(assetPath);
            }

            return list.ToArray();
        }

        /// <summary>
        /// 多个 Prefab 资源同名时警告：运行时按预制体名登记，后写覆盖先写。
        /// </summary>
        public static void WarnDuplicatePrefabNames(string[] prefabPaths)
        {
            if (prefabPaths == null || prefabPaths.Length == 0)
                return;

            var firstPathByName = new Dictionary<string, string>();
            for (var i = 0; i < prefabPaths.Length; i++)
            {
                var assetPath = prefabPaths[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                    continue;

                if (firstPathByName.TryGetValue(prefab.name, out var firstPath))
                {
                    Debug.LogWarning(
                        $"多个 Prefab 同名，后登记将覆盖先登记：{prefab.name}，先：{firstPath}，后：{assetPath}");
                    continue;
                }

                firstPathByName.Add(prefab.name, assetPath);
            }
        }
    }
}
