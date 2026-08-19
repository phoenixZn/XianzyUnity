#if !CONSOLE_CLIENT
using UnityEngine;
#endif

namespace Xease
{
#if !CONSOLE_CLIENT
    /// <summary>
    /// Unity 登入态：OnGUI「开始」按钮切入 ES_Main。
    /// </summary>
    public class EnvLoginState : EnvStateBase, IEnvOnGUI, IEnvUpdate
    {
        //////////////////////////////////////////////////////////////////////////
        /// EnvStateBase：override
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
        }

        public override void Leave(EnvStateBase toState)
        {
            base.Leave(toState);
        }

        public override string CheckTransitions()
        {
            return base.CheckTransitions();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEnvUpdate:
        /// <summary>
        /// Unity 登入态无逐帧逻辑。
        /// </summary>
        public void EnvUpdate(float dt, float dt_unscaled)
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEnvOnGUI:
        const float ButtonWidth = 480f;
        const float ButtonHeight = 112f;
        const int ButtonFontSize = 48;
        const float VerticalRatio = 3f / 5f; // 距顶部 3/5，即距底部约 2/5 屏高

        static GUIStyle _loginButtonStyle; // 缓存「开始」按钮样式，避免每帧 new
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

        /// <summary>
        /// 绘制中央「开始」按钮；点击后切到 ES_Main。
        /// </summary>
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
#else
    /// <summary>
    /// 命令行登入态：打印提示，读到非空行后切到 ES_Main。
    /// </summary>
    public class EnvLoginState : EnvStateBase, IEnvUpdate
    {
        //////////////////////////////////////////////////////////////////////////
        /// EnvStateBase：override
        /// <summary>
        /// 进入登入态时打印一次输入提示。
        /// </summary>
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
            System.Console.WriteLine("请输入任意非空字符串后回车以登入：");
        }

        public override void Leave(EnvStateBase toState)
        {
            base.Leave(toState);
        }

        public override string CheckTransitions()
        {
            return base.CheckTransitions();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEnvUpdate:
        /// <summary>
        /// 有完整输入行且非空时设置下一状态为 ES_Main；空行则再提示并留下。
        /// </summary>
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            if (_nextStateID != null)
                return;
            if (!System.Console.KeyAvailable)
                return;

            var line = System.Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line))
            {
                System.Console.WriteLine("输入不能为空，请重新输入：");
                return;
            }

            _nextStateID = EnvStateID.ES_Main;
        }
    }
#endif
}
