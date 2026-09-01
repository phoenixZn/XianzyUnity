using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ModelPointTool.Editor
{
    /// <summary>
    /// 模型挂点生成配置：模型根目录与要扫描的挂点名。仅编辑器使用。
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
        [Tooltip("直接拖拽文件夹到这里")]
        // 扫描根，必须是文件夹
        public DefaultAsset ModelRootPath;

        [Header("文件夹路径（只读）")]
        [SerializeField, TextArea(1, 2)]
        // 与 ModelRootPath 同步的 Asset 路径，供 Inspector 展示
        string folderPath;

        /// <summary>
        /// 当前模型根的 Asset 路径；未指定时为空。
        /// </summary>
        public string FolderPath
        {
            get
            {
                if (ModelRootPath != null)
                    return AssetDatabase.GetAssetPath(ModelRootPath);
                return folderPath;
            }
        }

        void OnValidate()
        {
            if (ModelRootPath == null)
            {
                folderPath = string.Empty;
                return;
            }

            var path = AssetDatabase.GetAssetPath(ModelRootPath);
            if (AssetDatabase.IsValidFolder(path))
            {
                folderPath = path;
                return;
            }

            Debug.LogWarning("请选择文件夹而不是文件！");
            ModelRootPath = null;
            folderPath = string.Empty;
        }

        /// <summary>
        /// 收集模型根下全部 Prefab 的 Asset 路径。
        /// </summary>
        public string[] CollectPrefabPaths()
        {
            if (ModelRootPath == null)
                return System.Array.Empty<string>();

            var folder = AssetDatabase.GetAssetPath(ModelRootPath);
            if (string.IsNullOrEmpty(folder) || !AssetDatabase.IsValidFolder(folder))
                return System.Array.Empty<string>();

            return CollectPrefabPathsInFolder(folder);
        }

        /// <summary>
        /// 收集指定文件夹下全部 Prefab 的 Asset 路径。
        /// </summary>
        public static string[] CollectPrefabPathsInFolder(string folder)
        {
            var list = new List<string>();
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { folder });
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
    }
}
