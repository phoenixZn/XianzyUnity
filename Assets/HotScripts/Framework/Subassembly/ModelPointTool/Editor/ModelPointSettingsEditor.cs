using UnityEditor;
using UnityEngine;

namespace ModelPointTool.Editor
{
    /// <summary>
    /// ModelPointSettings 的 Inspector：多文件夹、挂点列表与生成入口。
    /// </summary>
    [CustomEditor(typeof(ModelPointSettings))]
    public class ModelPointSettingsEditor : UnityEditor.Editor
    {
        SerializedProperty _nameListProperty; // BindPointNameList
        SerializedProperty _rootPathsProperty; // ModelRootPaths
        Vector2 _nameListScroll; // 挂点名列表滚动
        Vector2 _prefabNameScroll; // Prefab 名列表滚动
        Vector2 _rootListScroll; // 模型根列表滚动

        void OnEnable()
        {
            _nameListProperty = serializedObject.FindProperty("BindPointNameList");
            _rootPathsProperty = serializedObject.FindProperty("ModelRootPaths");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            var config = (ModelPointSettings)target;

            EditorGUILayout.Space();
            DrawRootFolderList();
            DrawPrefabList(config);
            DrawBindPointList();

            EditorGUILayout.Space();
            if (GUILayout.Button("生成模型挂点"))
            {
                serializedObject.ApplyModifiedProperties();
                GenerateModelPoint.Generate();
            }

            serializedObject.ApplyModifiedProperties();
        }

        void DrawRootFolderList()
        {
            if (_rootPathsProperty == null)
                return;

            EditorGUILayout.LabelField("文件夹选择", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"模型根目录数量: {_rootPathsProperty.arraySize}");
            if (GUILayout.Button("添加", GUILayout.Width(60)))
                _rootPathsProperty.arraySize++;
            if (GUILayout.Button("清空全部", GUILayout.Width(80)))
                _rootPathsProperty.arraySize = 0;
            EditorGUILayout.EndHorizontal();

            _rootListScroll = EditorGUILayout.BeginScrollView(_rootListScroll, GUILayout.Height(220));
            for (var i = 0; i < _rootPathsProperty.arraySize; i++)
            {
                var element = _rootPathsProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                EditorGUILayout.PropertyField(element, GUIContent.none);
                if (GUILayout.Button("×", GUILayout.Width(20)))
                {
                    DeleteObjectArrayElement(_rootPathsProperty, i);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    break;
                }

                EditorGUILayout.EndHorizontal();

                var folder = element.objectReferenceValue as DefaultAsset;
                var path = folder != null ? AssetDatabase.GetAssetPath(folder) : string.Empty;
                var isValid = !string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path);

                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("文件夹路径", path);
                EditorGUI.EndDisabledGroup();

                if (folder != null && !isValid)
                    EditorGUILayout.HelpBox("当前引用不是有效的文件夹！", MessageType.Warning);

                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(!isValid);
                if (GUILayout.Button("在资源管理器中显示"))
                    EditorUtility.RevealInFinder(path);
                EditorGUI.EndDisabledGroup();
                if (GUILayout.Button("清空"))
                    element.objectReferenceValue = null;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        void DrawPrefabList(ModelPointSettings config)
        {
            var folders = config.GetValidFolderPaths();
            if (folders.Length == 0)
                return;

            var prefabPaths = ModelPointSettings.CollectPrefabPathsInFolders(folders);
            EditorGUILayout.LabelField($"检测到的 Prefab 数量: {prefabPaths.Length}");

            _prefabNameScroll = EditorGUILayout.BeginScrollView(_prefabNameScroll, GUILayout.Height(200));
            for (var i = 0; i < prefabPaths.Length; i++)
            {
                var assetPath = prefabPaths[i];
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                if (prefab == null)
                    continue;

                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"{i + 1}.", GUILayout.Width(30));
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField(prefab.name, GUILayout.Width(140));
                EditorGUILayout.TextField(assetPath);
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

        // Unity 对 Object 数组第一次 Delete 只清空引用，第二次才移除元素
        static void DeleteObjectArrayElement(SerializedProperty array, int index)
        {
            var element = array.GetArrayElementAtIndex(index);
            if (element.objectReferenceValue != null)
                array.DeleteArrayElementAtIndex(index);
            array.DeleteArrayElementAtIndex(index);
        }
    }
}
