#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using Vector2 = UnityEngine.Vector2;

namespace IGTools
{
    
    public class CoreGameToolsWindow : EditorWindow
    {
        // CONST
        private const string TITLE = "InGame Tools";
        // VARIABLE
        private int tabIndex = 0;
        
        
        private EditorRuntimeToolBase[] _barTools;
        private string[] _barNames;
        
        [MenuItem("工具箱/战斗调试工具 %#i")]
        private static void ShowWindow()
        {
            var window = GetWindow<CoreGameToolsWindow>();
            window.titleContent = new GUIContent("战斗测试");
            window.minSize=new Vector2(300,600);
            window.Show();
        }
        

        private void OnEnable()
        {
            _barTools = new EditorRuntimeToolBase[]
            {
                new LevelTestTool(this),
                new SupplyTestTool(this),
                new EnemyTestTool(this),
            };
            _barNames = new string[_barTools.Length];
            for (int i = 0; i < _barTools.Length; i++)
            {
                _barTools[i].InitTool();
                _barNames[i] = _barTools[i].ToolName;
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginVertical();
            DrawHeader();
            tabIndex = GUILayout.Toolbar(tabIndex, _barNames,GUILayout.Height(30));
            _barTools[tabIndex].DrawTool();
            
            GUILayout.EndVertical();
        }
        
        private void DrawHeader()
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontStyle = FontStyle.Italic;
            labelStyle.fontSize = 20;
            GUILayout.BeginHorizontal();
            GUILayout.Label($"游戏状态:", GUILayout.Height(25));
            GUIStyle stateLabel = new GUIStyle(GUI.skin.label);
            var state = GetGameStatus();
            stateLabel.normal.textColor = state.Item2;
            GUILayout.Label(state.Item3, stateLabel, GUILayout.Height(25));
            GUILayout.FlexibleSpace();
            GUILayout.Label($"{Application.productName}", labelStyle, GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
        }

        private (int, Color, string) GetGameStatus()
        {
            GUIStyle stateLabel = new GUIStyle(GUI.skin.label);
            if (!Application.isPlaying)
            {
                return (0, Color.red, "未启动");
            }
            else
            {
                return (1, Color.green, "启动");
            }
        }
        
    }
    
}
#endif