#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 功能块的编辑器视图。在 GM 窗口滚动列表里画自己的一块。
    /// </summary>
    public interface IGmEditorDrawer
    {
        /// <summary>绘制本块。由编辑器窗口在滚动视图内调用。</summary>
        void OnGUI();
    }

    /// <summary>
    /// GM 编辑器窗口。滚动列出各功能块的 <see cref="IGmEditorDrawer"/>，不保存块的配置。
    /// </summary>
    public class GmEditorWindow : EditorWindow
    {
        Vector2 _scroll;

        /// <summary>打开 GM 窗口。</summary>
        [MenuItem("工具箱/GM")]
        public static void Open()
        {
            var window = GetWindow<GmEditorWindow>("GM");
            window.minSize = new Vector2(360f, 240f);
            window.Show();
        }

        void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnGUI()
        {
            var running = Application.isPlaying && GEnv.Inst != null;
            EditorGUI.BeginDisabledGroup(!running);
            if (GUILayout.Button("打开运行时 GM 窗"))
                G.Module<ModuleGM>()?.ShowCanvas();
            EditorGUI.EndDisabledGroup();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (!running)
                EditorGUILayout.HelpBox("需进入播放模式", MessageType.Info);
            else
                DrawBlocks();
            EditorGUILayout.EndScrollView();
        }

        static void DrawBlocks()
        {
            var module = G.Module<ModuleGM>();
            if (module == null)
            {
                EditorGUILayout.HelpBox("GM 模块未就绪", MessageType.Warning);
                return;
            }

            var blocks = module.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is IGmEditorDrawer drawer)
                    drawer.OnGUI();
            }
        }
    }
}
#endif
