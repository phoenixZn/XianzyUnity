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
        public void OnEnvGUI()
        {
            // 开始一个垂直布局组，自动居中或放在左上角（取决于需求）
            GUILayout.BeginArea(new Rect(300, 800, 300, 200));
            GUILayout.BeginVertical("box"); // 带边框的垂直组
            

            // 创建一个按钮，如果被点击就增加计数
            if (GUILayout.Button("登陆"))
            {
                _nextStateID = EnvStateID.ES_Main;
            }

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
}