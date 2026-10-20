#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 功能块的编辑器视图。在 GM 窗口当前页里画自己的一块。页签名见 <see cref="IGmBlock.Tab"/>。
    /// </summary>
    public interface IGmEditorDrawer
    {
        /// <summary>绘制本块。由编辑器窗口在滚动视图内调用。</summary>
        void OnGUI();
    }

    /// <summary>
    /// GM 编辑器窗口。顶条按页签分页，滚动列出当前页的 <see cref="IGmEditorDrawer"/>，不保存块的配置。
    /// </summary>
    public class GmEditorWindow : EditorWindow
    {
        const float TabBarHeight = 30f; // 顶条高度，像素

        Vector2 _scroll;
        string _selectedTab; // 当前页签名，窗口存活期间保持选择

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

            if (running)
                DrawTabBar();

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (!running)
                EditorGUILayout.HelpBox("需进入播放模式", MessageType.Info);
            else
                DrawBlocks();
            EditorGUILayout.EndScrollView();
        }

        void DrawTabBar()
        {
            var tabs = CollectTabs();
            if (tabs.Length == 0)
                return;

            var index = IndexOf(tabs, _selectedTab);
            // 原选择不在本次页签里时回到第一页
            if (index < 0)
                index = 0;
            index = GUILayout.Toolbar(index, tabs, GUILayout.Height(TabBarHeight));
            _selectedTab = tabs[index];
        }

        void DrawBlocks()
        {
            var module = G.Module<ModuleGM>();
            if (module == null)
            {
                EditorGUILayout.HelpBox("GM 模块未就绪", MessageType.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_selectedTab))
                return;

            var blocks = module.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is IGmEditorDrawer drawer && blocks[i].Tab == _selectedTab)
                    drawer.OnGUI();
            }
        }

        // 按目录首次出现去重；空页签名不进顶条
        static string[] CollectTabs()
        {
            var module = G.Module<ModuleGM>();
            if (module == null)
                return System.Array.Empty<string>();

            var blocks = module.Blocks;
            var names = new List<string>();
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is not IGmEditorDrawer)
                    continue;
                var tab = blocks[i].Tab;
                if (string.IsNullOrEmpty(tab) || names.Contains(tab))
                    continue;
                names.Add(tab);
            }

            return names.ToArray();
        }

        static int IndexOf(string[] tabs, string selected)
        {
            if (string.IsNullOrEmpty(selected))
                return -1;

            for (int i = 0; i < tabs.Length; i++)
            {
                if (tabs[i] == selected)
                    return i;
            }

            return -1;
        }
    }
}
#endif
