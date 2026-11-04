#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Xease
{
    public partial class GmGameSpeed : IGmEditorDrawer
    {
        //////////////////////////////////////////////////////////////////////////
        /// IGmEditorDrawer:

        /// <summary>在编辑器滚动列表中画本块。未播放时按钮不可用，高亮读当前基准倍速。</summary>
        public void OnGUI()
        {
            var running = Application.isPlaying && GEnv.Inst != null;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(Title);
            if (running)
                EditorGUILayout.LabelField("当前 " + FormatSpeed(Current));

            EditorGUI.BeginDisabledGroup(!running);
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < Presets.Length; i++)
            {
                var preset = Presets[i];
                var previous = GUI.backgroundColor;
                if (running && IsCurrent(preset))
                    GUI.backgroundColor = new Color(0.45f, 0.75f, 0.45f, 1f);
                if (GUILayout.Button("x" + FormatSpeed(preset), GUILayout.Height(32f)))
                    TrySet(preset);
                GUI.backgroundColor = previous;
            }

            EditorGUILayout.EndHorizontal();
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }
    }
}
#endif
