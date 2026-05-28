#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace XEditor.UnityPartingTool
{
    /// <summary>
    /// Editor window for separating and restoring Unity-specific code.
    /// </summary>
    public sealed class UnityPartingWindow : EditorWindow
    {
        // UnityParting：配置 Root/Target，一键分离或还原「.Unity」目录与文件（剪切，非复制）。
        private const string WindowTitle = "UnityParting";
        private const string DefaultRootFolderPath = "Assets/HotScripts";
        private const string DefaultTargetFolderPath = "Assets/HotScripts/UnityParting";
        private const string RootFolderPrefsKey = "XEditor.UnityParting.RootFolderPath";
        private const string TargetFolderPrefsKey = "XEditor.UnityParting.TargetFolderPath";
        private const string LegacyRootPrefix = "Assets/HotUpdateScripts";

        private DefaultAsset _rootFolder;
        private DefaultAsset _targetFolder;
        private string _rootFolderPath = DefaultRootFolderPath;
        private string _targetFolderPath = DefaultTargetFolderPath;
        private string _lastMessage = "请选择 Root 与 Target 文件夹，然后执行分离或还原。";
        private MessageType _lastMessageType = MessageType.Info;

        // 菜单入口：独立 Utility 窗口，不打断主窗口焦点太多。
        [MenuItem("XTools/打开 UnityParting 工具窗口", priority = 0)]
        public static void OpenWindow()
        {
            UnityPartingWindow window = GetWindow<UnityPartingWindow>(true, WindowTitle, true);
            window.minSize = new Vector2(680f, 260f);
            window.ShowUtility();
            window.Focus();
        }

        private void OnEnable()
        {
            // 读 EditorPrefs、补全默认 Target 文件夹引用。
            LoadState();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("UnityParting", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "分离时会扫描 Root 文件夹中名称以 .Unity 结尾的目录，以及文件名（去掉扩展名后）以 .Unity 结尾的文件，并剪切到 Target 文件夹，同时保持相对 Root 的目录结构。移动会通过 Unity 资源接口执行，确保 .cs、文件夹及其 .meta 一起迁移。",
                MessageType.Info);

            EditorGUILayout.Space(4f);
            DrawFolderField("Root 文件夹", ref _rootFolder, ref _rootFolderPath, DefaultRootFolderPath, false);
            DrawFolderField("Target 文件夹", ref _targetFolder, ref _targetFolderPath, DefaultTargetFolderPath, true);

            EditorGUILayout.Space(8f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("分离Unity代码", GUILayout.Height(32f)))
                {
                    ExecuteSeparate();
                }

                if (GUILayout.Button("还原Unity代码", GUILayout.Height(32f)))
                {
                    ExecuteRestore();
                }
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.HelpBox(_lastMessage, _lastMessageType);
        }

        // ObjectField 只接受文件夹（DefaultAsset）；createIfMissing 为 true 时清引用会回到默认路径并创建磁盘文件夹。
        private void DrawFolderField(
            string label,
            ref DefaultAsset folderAsset,
            ref string folderPath,
            string defaultPath,
            bool createIfMissing)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUI.BeginChangeCheck();
            DefaultAsset selectedFolder = (DefaultAsset)EditorGUILayout.ObjectField(label, folderAsset, typeof(DefaultAsset), false);
            if (EditorGUI.EndChangeCheck())
            {
                if (selectedFolder == null)
                {
                    // 用户清空引用：回到默认路径（Target 会 Ensure存在，Root 不自动建目录）。
                    folderPath = defaultPath;
                    if (createIfMissing)
                    {
                        UnityPartingProcessor.EnsureAssetFolderExists(folderPath);
                    }

                    folderAsset = LoadFolderAsset(folderPath);
                    SaveState();
                }
                else if (TryGetFolderPath(selectedFolder, out string selectedPath))
                {
                    folderAsset = selectedFolder;
                    folderPath = selectedPath;
                    SaveState();
                }
                else
                {
                    EditorUtility.DisplayDialog("无效目录", $"{label} 必须引用 Project 视图中的文件夹。", "确定");
                }
            }

            using (new EditorGUI.DisabledGroupScope(true))
            {
                // 只读展示实际使用的 Assets 路径字符串，和拖拽引用一致。
                EditorGUILayout.TextField("当前路径", folderPath);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("恢复默认", GUILayout.Width(92f)))
            {
                folderPath = defaultPath;
                if (createIfMissing)
                {
                    UnityPartingProcessor.EnsureAssetFolderExists(folderPath);
                }

                folderAsset = LoadFolderAsset(folderPath);
                SaveState();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        // 调用处理器：Root -> Target 剪切匹配资源。
        private void ExecuteSeparate()
        {
            try
            {
                UnityPartingOperationResult result = UnityPartingProcessor.SeparateUnityCode(_rootFolderPath, _targetFolderPath);
                _lastMessage = result.Message;
                _lastMessageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                _lastMessage = $"分离失败：{exception.Message}";
                _lastMessageType = MessageType.Error;
                Debug.LogException(exception);
            }
        }

        // Target -> Root 剪切还原；二次确认防止误触覆盖。
        private void ExecuteRestore()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "确认还原Unity代码",
                "该操作会将 Target 文件夹中的内容剪切回 Root 文件夹，是否继续？",
                "继续",
                "取消");

            if (!confirmed)
            {
                return;
            }

            try
            {
                UnityPartingOperationResult result = UnityPartingProcessor.RestoreUnityCode(_rootFolderPath, _targetFolderPath);
                _lastMessage = result.Message;
                _lastMessageType = MessageType.Info;
            }
            catch (Exception exception)
            {
                _lastMessage = $"还原失败：{exception.Message}";
                _lastMessageType = MessageType.Error;
                Debug.LogException(exception);
            }
        }

        // 启动时恢复上次路径；Target 默认路径若不存在则创建，保证能拖引用。
        private void LoadState()
        {
            _rootFolderPath = EditorPrefs.GetString(RootFolderPrefsKey, DefaultRootFolderPath);
            if (string.IsNullOrWhiteSpace(_rootFolderPath))
            {
                _rootFolderPath = DefaultRootFolderPath;
            }
            else if (_rootFolderPath.StartsWith(LegacyRootPrefix, StringComparison.Ordinal))
            {
                _rootFolderPath = DefaultRootFolderPath
                    + _rootFolderPath.Substring(LegacyRootPrefix.Length);
            }

            _targetFolderPath = EditorPrefs.GetString(TargetFolderPrefsKey, DefaultTargetFolderPath);
            if (string.IsNullOrWhiteSpace(_targetFolderPath))
            {
                _targetFolderPath = DefaultTargetFolderPath;
            }
            else if (_targetFolderPath.StartsWith(LegacyRootPrefix, StringComparison.Ordinal))
            {
                _targetFolderPath = DefaultRootFolderPath
                    + _targetFolderPath.Substring(LegacyRootPrefix.Length);
            }

            UnityPartingProcessor.EnsureAssetFolderExists(_targetFolderPath);
            _rootFolder = LoadFolderAsset(_rootFolderPath);
            _targetFolder = LoadFolderAsset(_targetFolderPath);
            SaveState();
        }

        // 持久化两个文件夹路径，下次打开编辑器仍记住。
        private void SaveState()
        {
            EditorPrefs.SetString(RootFolderPrefsKey, _rootFolderPath);
            EditorPrefs.SetString(TargetFolderPrefsKey, _targetFolderPath);
        }

        // 从 Assets 路径加载文件夹资源，用于 ObjectField 显示。
        private static DefaultAsset LoadFolderAsset(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
        }

        // 校验拖拽对象确实是工程里的文件夹（不是场景对象或其它资源）。
        private static bool TryGetFolderPath(DefaultAsset folderAsset, out string folderPath)
        {
            folderPath = string.Empty;
            if (folderAsset == null)
            {
                return false;
            }

            string assetPath = AssetDatabase.GetAssetPath(folderAsset);
            if (string.IsNullOrWhiteSpace(assetPath) || !AssetDatabase.IsValidFolder(assetPath))
            {
                return false;
            }

            folderPath = assetPath;
            return true;
        }
    }
}
#endif
