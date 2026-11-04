#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Xease
{
    public partial class GmStaticAction : IGmEditorDrawer
    {
        //////////////////////////////////////////////////////////////////////////
        /// IGmEditorDrawer:

        /// <summary>在编辑器当前页按登记顺序画按钮。未播放时不可点。</summary>
        public void OnGUI()
        {
            var running = Application.isPlaying && GEnv.Inst != null;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(Title);

            EditorGUI.BeginDisabledGroup(!running);
            for (int i = 0; i < _entries.Count; i++)
            {
                var entry = _entries[i];
                if (GUILayout.Button(entry.Name, GUILayout.Height(32f)))
                    entry.Action();
            }

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
