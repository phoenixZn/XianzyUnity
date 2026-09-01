using UnityEditor;
using UnityEngine;

namespace ModelPointTool.Editor
{
    /// <summary>
    /// ModelPointSettings 的 Inspector：选文件夹、挂点列表与生成入口。
    /// </summary>
    [CustomEditor(typeof(ModelPointSettings))]
    public class ModelPointSettingsEditor : UnityEditor.Editor
    {
        SerializedProperty _nameListProperty; // BindPointNameList
        Vector2 _nameListScroll; // 挂点名列表滚动
        Vector2 _prefabNameScroll; // Prefab 名列表滚动

        void OnEnable()
        {
            _nameListProperty = serializedObject.FindProperty("BindPointNameList");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (ModelPointSettings)target;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("文件夹选择", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("ModelRootPath"),
                new GUIContent("目标文件夹"));

            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.TextField("文件夹路径", config.FolderPath);
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("在资源管理器中显示"))
            {
                if (!string.IsNullOrEmpty(config.FolderPath))
                    EditorUtility.RevealInFinder(config.FolderPath);
            }

            if (GUILayout.Button("清空引用"))
            {
                serializedObject.FindProperty("ModelRootPath").objectReferenceValue = null;
                serializedObject.FindProperty("folderPath").stringValue = string.Empty;
            }

            EditorGUILayout.EndHorizontal();

            DrawPrefabList(config);
            DrawBindPointList();

            EditorGUILayout.Space();
            if (GUILayout.Button("生成模型挂点"))
                GenerateModelPoint.Generate();

            serializedObject.ApplyModifiedProperties();
        }

        void DrawPrefabList(ModelPointSettings config)
        {
            if (config.ModelRootPath == null)
                return;

            var path = AssetDatabase.GetAssetPath(config.ModelRootPath);
            if (!AssetDatabase.IsValidFolder(path))
            {
                EditorGUILayout.HelpBox("当前引用不是有效的文件夹！", MessageType.Warning);
                return;
            }

            var prefabPaths = ModelPointSettings.CollectPrefabPathsInFolder(path);
            EditorGUILayout.LabelField($"检测到的 Prefab 数量: {prefabPaths.Length}");

            _prefabNameScroll = EditorGUILayout.BeginScrollView(_prefabNameScroll, GUILayout.Height(200));
            for (var i = 0; i < prefabPaths.Length; i++)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPaths[i]);
                if (prefab == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(prefab.name);
                EditorGUI.EndDisabledGroup();
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawBindPointList()
        {
            if (_nameListProperty == null)
                return;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"挂点数量: {_nameListProperty.arraySize}");
            if (GUILayout.Button("添加", GUILayout.Width(60)))
                _nameListProperty.arraySize++;
            if (GUILayout.Button("清空", GUILayout.Width(60)))
                _nameListProperty.arraySize = 0;
            EditorGUILayout.EndHorizontal();

            _nameListScroll = EditorGUILayout.BeginScrollView(_nameListScroll, GUILayout.Height(200));
            for (var i = 0; i < _nameListProperty.arraySize; i++)
            {
                var element = _nameListProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                EditorGUILayout.PropertyField(element, GUIContent.none);
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    _nameListProperty.DeleteArrayElementAtIndex(i);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
