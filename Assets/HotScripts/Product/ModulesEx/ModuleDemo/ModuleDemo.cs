using UnityEngine;

namespace Xease
{
    //[SkipModuleAutoRegister]
    public class ModuleDemo : Module, IEnvOnGUI
    {
        public ModuleDemo()
        {
            G.LogWarning("ModDemo 构造");
        }
        
        protected override void OnInit()
        {
            G.LogWarning("ModDemo OnInit");
            base.OnInit();
        }

        protected override void OnShutdown()
        {
            G.LogWarning("ModDemo OnShutdown");
            base.OnShutdown();
        }


        //////////////////////////////////////////////////////////////////////////
        /// Test GUI
        private int clickCount = 0;
        public void OnEnvGUI()
        {
            // 复制当前的皮肤，避免修改原始资源
            GUISkin mySkin = Object.Instantiate(GUI.skin);
            // 设置各控件的默认字体大小
            mySkin.label.fontSize = 32;
            mySkin.button.fontSize = 32;
            mySkin.textField.fontSize = 32;
            // 应用这个皮肤
            GUI.skin = mySkin;
            
            // 开始一个垂直布局组，自动居中或放在左上角（取决于需求）
            GUILayout.BeginArea(new Rect(100, 10, 600, 600));
            GUILayout.BeginVertical("box"); // 带边框的垂直组

            // 显示当前点击次数
            GUILayout.Label($"按钮被点击了 {clickCount} 次");

            // 创建一个按钮，如果被点击就增加计数
            if (GUILayout.Button("点我增加计数"))
            {
                clickCount++;
                Debug.Log($"按钮被点击，当前计数：{clickCount}");
            }
            
            DrawFoldout_Custom();

            OnGUI_Scrol();
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
        
                

        private void DrawFoldout_Custom()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Space(20);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 6; i++)
            {
                if (GUILayout.Button($"{i}", GUILayout.Height(30), GUILayout.Width(60)))
                {
                    Debug.Log($"DrawFoldout_Custom：{i}");
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);
            GUILayout.Space(20);
        }
        
        
        // 这个Vector2变量用来记录和恢复滚动视图的滚动位置，必须持有一个引用
        private Vector2 scrollPosition;

        private void OnGUI_Scrol()
        {
            // 1. 定义滚动视图的区域，这里设置宽200，高300
            //    如果省略宽高，它会尝试填满可用空间
            scrollPosition = GUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Width(500),
                GUILayout.Height(300)
            );

            // 2. 在这个区域内，就可以像平时一样，自由放置任意数量的控件了
            for (int i = 0; i < 50; i++)
            {
                GUILayout.Button($"这是滚动列表里的按钮 {i+1}");
            }

            // 3. 结束滚动视图
            GUILayout.EndScrollView();
        }
    }
}