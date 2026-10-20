using System;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xease
{
    /// <summary>
    /// 功能块的命令行视图。由模块统一分发，块只处理自己的命令键。
    /// </summary>
    public interface IGmConsoleBlock
    {
        /// <summary>把本块命令登记进 <see cref="ModuleGM.CommandGroup"/>。</summary>
        void RegisterConsole();

        /// <summary>
        /// 处理一条命令。
        /// </summary>
        /// <param name="raw">控制台原文。</param>
        /// <returns>命令键属于本块时为 true，无论参数是否被接受。</returns>
        bool TryExecute(string raw);
    }

    /// <summary>
    /// 功能块的真机调试窗视图。只负责往滚动内容里加自己的一块。
    /// </summary>
    public interface IGmCanvasBlock
    {
        /// <summary>在调试窗滚动内容下追加本块。</summary>
        /// <param name="content">滚动内容根节点。</param>
        void AddToCanvas(RectTransform content);
    }

    public partial class ModuleGM
    {
        GmDebugCanvas _canvas; // 代码生成的调试窗，关闭模块时销毁

        /// <summary>生成或重新显示运行时 GM 窗。窗体只搭一次，各块在滚动列表里各占一块。</summary>
        public void ShowCanvas()
        {
            if (_canvas == null || _canvas.Root == null)
                _canvas = GmDebugCanvas.Create(this);

            _canvas.Root.SetActive(true);
        }

        partial void BindRuntimeViews()
        {
            DebugLogConsole.SetSpecCommandHandler(CommandGroup, DispatchCommand);
            DebugLogConsole.AddCommand_Spec(ShowCanvasCommand, CommandGroup, "打开 GM 调试窗");
            for (int i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i] is IGmConsoleBlock consoleBlock)
                    consoleBlock.RegisterConsole();
            }
        }

        partial void UnbindRuntimeViews()
        {
            DebugLogConsole.SetSpecCommandHandler(CommandGroup, IgnoreCommand);
            if (_canvas != null)
            {
                _canvas.Destroy();
                _canvas = null;
            }
        }

        bool DispatchCommand(string raw)
        {
            if (DebugLogConsole.ExtractCmdKey(raw) == ShowCanvasCommand)
            {
                ShowCanvas();
                return true;
            }

            for (int i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i] is IGmConsoleBlock consoleBlock && consoleBlock.TryExecute(raw))
                    return true;
            }

            return false;
        }

        static bool IgnoreCommand(string raw)
        {
            return false;
        }
    }

    /// <summary>
    /// 运行时 GM 窗。代码生成 Overlay 画布和滚动列表，不经过 UI 服务，也不持有任何功能块的配置。
    /// </summary>
    public sealed class GmDebugCanvas
    {
        const int CanvasSortOrder = 32000; // 高于界面最顶层，保证调试窗可点
        const string RootName = "GmDebugCanvas";

        /// <summary>Overlay 根物体，用来显示、隐藏和销毁调试窗。</summary>
        public GameObject Root { get; private set; }

        /// <summary>创建画布并让每个实现了画布视图的块往滚动内容里加一块。</summary>
        public static GmDebugCanvas Create(ModuleGM module)
        {
            var canvas = new GmDebugCanvas();
            canvas.Build(module);
            return canvas;
        }

        /// <summary>销毁画布根节点。</summary>
        public void Destroy()
        {
            if (Root != null)
                UnityEngine.Object.Destroy(Root);
            Root = null;
        }

        void Build(ModuleGM module)
        {
            EnsureEventSystem();

            var root = new GameObject(RootName, typeof(RectTransform));
            Root = root;
            GmRuntimeUi.Stretch(root.GetComponent<RectTransform>());
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = CanvasSortOrder;
            canvas.vertexColorAlwaysGammaSpace = true;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
            root.AddComponent<GraphicRaycaster>();

            var panel = GmRuntimeUi.CreateRect("Panel", root.transform);
            panel.anchorMin = new Vector2(0.52f, 0.08f);
            panel.anchorMax = new Vector2(0.98f, 0.92f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;
            var panelImage = panel.gameObject.AddComponent<Image>();
            panelImage.sprite = GmRuntimeUi.WhiteSprite;
            panelImage.color = new Color(0.08f, 0.08f, 0.1f, 0.94f);
            var panelLayout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(16, 16, 16, 16);
            panelLayout.spacing = 12;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlWidth = true;
            panelLayout.childControlHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childForceExpandHeight = false;

            var titleRow = GmRuntimeUi.CreateRect("TitleRow", panel);
            var titleLayout = titleRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            titleLayout.spacing = 12;
            titleLayout.childAlignment = TextAnchor.MiddleLeft;
            titleLayout.childControlWidth = true;
            titleLayout.childControlHeight = true;
            titleLayout.childForceExpandWidth = false;
            titleLayout.childForceExpandHeight = true;
            var titleRowElement = titleRow.gameObject.AddComponent<LayoutElement>();
            titleRowElement.preferredHeight = GmRuntimeUi.ButtonHeight;
            titleRowElement.minHeight = GmRuntimeUi.ButtonHeight;
            GmRuntimeUi.CreateText("Title", "GM", titleRow, GmRuntimeUi.TitleFontSize, true);
            var close = GmRuntimeUi.CreateButton("关闭", titleRow, () => root.SetActive(false));
            var closeElement = close.GetComponent<LayoutElement>();
            closeElement.flexibleWidth = 0;
            closeElement.preferredWidth = 180;

            var scrollRoot = GmRuntimeUi.CreateRect("Scroll", panel);
            var scrollElement = scrollRoot.gameObject.AddComponent<LayoutElement>();
            scrollElement.flexibleHeight = 1;
            scrollElement.minHeight = 240;
            var scroll = scrollRoot.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 30f;

            var viewport = GmRuntimeUi.CreateRect("Viewport", scrollRoot);
            GmRuntimeUi.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            scroll.viewport = viewport;

            var content = GmRuntimeUi.CreateRect("Content", viewport);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            var contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 12;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;

            var blocks = module.Blocks;
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i] is IGmCanvasBlock canvasBlock)
                    canvasBlock.AddToCanvas(content);
            }

            var uiLayer = LayerMask.NameToLayer("UI");
            if (uiLayer >= 0)
                SetLayer(root.transform, uiLayer);
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }

        static void SetLayer(Transform node, int layer)
        {
            node.gameObject.layer = layer;
            for (int i = 0; i < node.childCount; i++)
                SetLayer(node.GetChild(i), layer);
        }
    }

    /// <summary>
    /// 调试窗共用的运行时控件。只提供搭 UI 的手段，不保存功能块数据。
    /// </summary>
    static class GmRuntimeUi
    {
        internal const int TitleFontSize = 36; // 窗体标题字号
        internal const int BodyFontSize = 28; // 块内正文与按钮字号
        internal const int ButtonHeight = 72; // 预设按钮高度，像素

        static Font _font;
        static Sprite _white;

        internal static Sprite WhiteSprite
        {
            get
            {
                if (_white == null)
                {
                    var texture = Texture2D.whiteTexture;
                    _white = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }

                return _white;
            }
        }

        internal static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        internal static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        internal static Text CreateText(string name, string text, Transform parent, int fontSize, bool flexibleWidth = false, bool ignoreLayout = false)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var label = go.AddComponent<Text>();
            label.font = BuiltinFont;
            label.text = text;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;
            var element = go.AddComponent<LayoutElement>();
            element.minHeight = fontSize + 16;
            element.preferredHeight = fontSize + 16;
            element.flexibleWidth = flexibleWidth ? 1f : 0f;
            element.ignoreLayout = ignoreLayout;
            return label;
        }

        internal static Button CreateButton(string label, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject("Btn_" + label, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = WhiteSprite;
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            if (onClick != null)
                button.onClick.AddListener(onClick);

            var text = CreateText("Text", label, go.transform, BodyFontSize, false, true);
            text.alignment = TextAnchor.MiddleCenter;
            Stretch(text.rectTransform);

            var element = go.AddComponent<LayoutElement>();
            element.minHeight = ButtonHeight;
            element.preferredHeight = ButtonHeight;
            element.flexibleWidth = 1f;
            return button;
        }

        static Font BuiltinFont
        {
            get
            {
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (_font == null)
                    _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                return _font;
            }
        }
    }
}
