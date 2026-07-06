using UnityEngine;

namespace Xease
{
    public class EnvLoginState : EnvStateBase, IEnvOnGUI, IEnvUpdate
    {


        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
        }
        
        public override void Leave(EnvStateBase toState)
        {
            base.Leave(toState);
        }
        
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            
        }
        
        public override string CheckTransitions()
        {
            return base.CheckTransitions();
        }

        //////////////////////////////////////////////////////////////////////////
        /// Test Code
        const float ButtonWidth = 480f;
        const float ButtonHeight = 112f;
        const int ButtonFontSize = 48;
        const float VerticalRatio = 3f / 5f; // 距顶部 3/5，即距底部约 2/5 屏高

        static GUIStyle _loginButtonStyle;
        static GUIStyle LoginButtonStyle
        {
            get
            {
                if (_loginButtonStyle != null)
                    return _loginButtonStyle;

                _loginButtonStyle = new GUIStyle(GUI.skin.button)
                {
                    fontSize = ButtonFontSize,
                    alignment = TextAnchor.MiddleCenter
                };
                return _loginButtonStyle;
            }
        }

        public void OnEnvGUI()
        {
            var x = (Screen.width - ButtonWidth) * 0.5f;
            var y = Screen.height * VerticalRatio - ButtonHeight * 0.5f;
            GUILayout.BeginArea(new Rect(x, y, ButtonWidth, ButtonHeight));
            if (GUILayout.Button("开始", LoginButtonStyle, GUILayout.Height(ButtonHeight)))
            {
                _nextStateID = EnvStateID.ES_Main;
            }
            GUILayout.EndArea();
        }
    }
}