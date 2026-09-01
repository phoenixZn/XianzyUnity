using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ModelPointTool.Editor
{
    /// <summary>
    /// 扫描模型 Prefab 上的挂点名，生成 ModelPointGetter.Gen.cs 中的登记代码。
    /// </summary>
    public static class GenerateModelPoint
    {
        // 生成结果固定写入热更侧 Gen 文件
        const string OutputAssetPath = "Assets/HotScripts/Product/Subassembly/ModelPointTool.Gen/ModelPointGetter.Gen.cs";
        // 项目侧配置，找不到 Settings 时在此创建
        const string DefaultSettingsAssetPath =
            "Assets/HotScripts/Product/Subassembly/ModelPointTool.Gen/ModelPointSettings.asset";

        // 生成文件头部（含 RegisterAll 起始）
        const string FileHeader =
            @"using Xease.ModelPointTool;

namespace Xease.ModelPointTool.Gen
{
    /// <summary>
    /// 编辑器生成的挂点路径表。手动修改会在下次生成时丢失。
    /// </summary>
    public static class ModelPointGetterGen
    {
        /// <summary>
        /// 清空并填入 (预制体名, 挂点名) → 相对路径。
        /// </summary>
        public static void RegisterAll()
        {
            ModelPointGetter.Clear();
";

        // 生成文件尾部
        const string FileFooter =
            @"        }
    }
}
";

        /// <summary>
        /// 按 Settings 扫描 Prefab 挂点并覆盖写入 ModelPointGetter.Gen.cs。
        /// </summary>
        [MenuItem("Assets/生成模型挂点")]
        public static void Generate()
        {
            var setting = GetOrCreateSetting();
            var prefabPaths = setting.CollectPrefabPaths();
            ModelPointSettings.WarnDuplicatePrefabNames(prefabPaths);
            var bindPoints = setting.BindPointNameList;
            var code = BuildGeneratedCode(prefabPaths, bindPoints);
            WriteGeneratedFile(code);
        }

        static ModelPointSettings GetOrCreateSetting()
        {
            var guids = AssetDatabase.FindAssets($"t:{nameof(ModelPointSettings)}");
            if (guids.Length == 0)
            {
                Debug.LogWarning($"Create new {nameof(ModelPointSettings)}.asset");
                var setting = ScriptableObject.CreateInstance<ModelPointSettings>();
                AssetDatabase.CreateAsset(setting, DefaultSettingsAssetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                return setting;
            }

            if (guids.Length != 1)
            {
                foreach (var guid in guids)
                    Debug.LogWarning($"Found multiple file : {AssetDatabase.GUIDToAssetPath(guid)}");
                throw new System.Exception($"Found multiple {nameof(ModelPointSettings)} files !");
            }

            return AssetDatabase.LoadAssetAtPath<ModelPointSettings>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        static string BuildGeneratedCode(string[] prefabPaths, string[] bindPoints)
        {
            var addBlock = new StringBuilder();
            if (prefabPaths == null || bindPoints == null)
            {
                Debug.LogError("ModelPointSettings is incomplete. Check model root paths and bind point list.");
                return FileHeader + addBlock + FileFooter;
            }

            var addCount = 0;
            for (var i = 0; i < prefabPaths.Length; i++)
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (obj == null)
                {
                    Debug.LogError($"Generate model point failed: invalid or unloaded prefab path = {prefabPaths[i]}");
                    continue;
                }

                for (var j = 0; j < bindPoints.Length; j++)
                {
                    var pointName = bindPoints[j];
                    if (string.IsNullOrEmpty(pointName))
                        continue;

                    var relativePath = ModelPointFinder.Search(obj.transform, pointName);
                    if (string.IsNullOrEmpty(relativePath))
                        continue;

                    addBlock.Append("            ModelPointGetter.Add(\"")
                        .Append(EscapeCsString(obj.name)).Append("\", \"")
                        .Append(EscapeCsString(pointName)).Append("\", \"")
                        .Append(EscapeCsString(relativePath)).Append("\");")
                        .AppendLine();
                    addCount++;
                }
            }

            Debug.Log($"生成模型挂点完成：登记 {addCount} 条，扫描 Prefab {prefabPaths.Length} 个。");
            return FileHeader + addBlock + FileFooter;
        }

        static void WriteGeneratedFile(string content)
        {
            var fullPath = Path.GetFullPath(Path.Combine(Application.dataPath,
                "HotScripts/Product/Subassembly/ModelPointTool.Gen/ModelPointGetter.Gen.cs"));
            var dir = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(fullPath, content, new UTF8Encoding(false));
            Debug.Log("脚本保存成功:" + OutputAssetPath);
            AssetDatabase.ImportAsset(OutputAssetPath);
            AssetDatabase.Refresh();
        }

        static string EscapeCsString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }
    }
}
