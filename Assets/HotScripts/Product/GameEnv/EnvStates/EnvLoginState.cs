#if !CONSOLE_CLIENT
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#endif

namespace Xease
{
#if !CONSOLE_CLIENT
    /// <summary>
    /// Unity 登入态：Enter 时拼一套临时 UGUI「开始」按钮，点击后切入 ES_Main。
    /// </summary>
    public class EnvLoginState : EnvStateBase
    {
        GameObject _loginUiRoot; // DDOL 临时 Canvas 根，Leave 时销毁

        //////////////////////////////////////////////////////////////////////////
        /// EnvStateBase：override
        /// <summary>
        /// 进入登入态时创建临时「开始」按钮 UI。
        /// </summary>
        public override void Enter(EnvStateBase fromState)
        {
            base.Enter(fromState);
            CreateLoginUi();
        }

        /// <summary>
        /// 离开登入态时销毁临时 UI。
        /// </summary>
        public override void Leave(EnvStateBase toState)
        {
            DestroyLoginUi();
            base.Leave(toState);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        // 运行时拼 Overlay Canvas +「开始」按钮；根节点 DDOL，避免异步切场景被清掉
        void CreateLoginUi()
        {
            DestroyLoginUi();

            if (EventSystem.current == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }

            var root = new GameObject("[LoginTempUI]");
            Object.DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            root.AddComponent<GraphicRaycaster>();

            var btnGo = new GameObject("StartButton");
            btnGo.transform.SetParent(root.transform, false);
            btnGo.AddComponent<Image>();
            btnGo.AddComponent<Button>().onClick.AddListener(() => _nextStateID = EnvStateID.ES_Main);
            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.4f);
            btnRt.sizeDelta = new Vector2(480f, 112f);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.text = "开始";
            txt.fontSize = 48;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.raycastTarget = false;
            var txtRt = txt.rectTransform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.sizeDelta = Vector2.zero;

            _loginUiRoot = root;
        }

        void DestroyLoginUi()
        {
            if (_loginUiRoot == null)
                return;
            Object.Destroy(_loginUiRoot);
            _loginUiRoot = null;
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
