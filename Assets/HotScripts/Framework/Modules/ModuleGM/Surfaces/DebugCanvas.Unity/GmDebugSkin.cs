using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 调试窗一段文字的字号、颜色和对齐。由 <see cref="GmDebugSkin"/> 按角色给出。
    /// </summary>
    struct GmTextStyle
    {
        public int FontSize; // 字号
        public Color Color; // 字色，须与所在底色明暗相反
        public TextAnchor Alignment; // 对齐
    }

    /// <summary>
    /// 调试窗一颗按钮的尺寸与各状态底色。字色在 <see cref="Label"/>，须亮于普通底色。
    /// </summary>
    struct GmButtonStyle
    {
        public float Height; // 高度，参考分辨率像素
        public float PreferredWidth; // 首选宽度；横向拉伸时由布局分配
        public bool FlexibleWidth; // 是否吃掉所在行剩余宽度
        public GmTextStyle Label; // 按钮文字
        public Color Normal; // 普通底色
        public Color Pressed; // 按下底色
        public Color Selected; // Selectable 选中态底色，应与普通底色同明暗
    }

    /// <summary>
    /// GM 调试窗皮肤。字号、间距、字体和颜色的唯一来源。画布与功能块只读 <see cref="Current"/>，建窗时生效。
    /// </summary>
    sealed class GmDebugSkin
    {
        public Font Font; // 覆盖字体；空则绘制时用内置字体

        public GmTextStyle WindowTitle; // 顶条窗体标题
        public GmTextStyle SectionTitle; // 功能区块标题
        public GmTextStyle Body; // 描述与状态正文，含倍速当前值

        public GmButtonStyle Tab; // 未选页签
        public GmButtonStyle TabSelected; // 当前页签
        public GmButtonStyle Close; // 顶条关闭
        public GmButtonStyle Action; // 区块内功能按钮
        public GmButtonStyle ActionSelected; // 当前选中的功能按钮

        public Color Panel; // 全屏面板底色
        public Color Section; // 功能区块底色

        public int PanelPadding; // 面板四边留白，参考分辨率像素
        public int PanelSpacing; // 面板与页内纵向间距
        public int SectionPadding; // 区块四边留白
        public int SectionSpacing; // 区块内纵向间距
        public int TabSpacing; // 页签间距
        public int RowSpacing; // 横排控件间距
        public int TextLinePadding; // 单行文字在字号之外追加的高度
        public float TitleRowHeight; // 顶条高度，参考分辨率像素

        /// <summary>内置紧凑皮肤。按钮为暗底亮字。字号与高度的相对大小见 <see cref="CreateCompact"/>。</summary>
        public static readonly GmDebugSkin Compact = CreateCompact();

        static GmDebugSkin _current = Compact;

        /// <summary>建窗时读取的皮肤。设为空则回退 <see cref="Compact"/>。</summary>
        public static GmDebugSkin Current
        {
            get => _current ?? Compact;
            set => _current = value;
        }

        static GmDebugSkin CreateCompact()
        {
            var label = new Color(0.96f, 0.97f, 0.98f, 1f); // 亮字，配暗底
            var body = new Color(0.75f, 0.78f, 0.82f, 1f); // 描述字，暗于标题
            var pressed = new Color(0.14f, 0.15f, 0.17f, 1f); // 按下时再压暗的中性底
            var green = new Color(0.22f, 0.48f, 0.32f, 1f); // 选中底
            var greenPressed = new Color(0.15f, 0.32f, 0.22f, 1f); // 选中后再按下
            var tabIdle = new Color(0.23f, 0.25f, 0.29f, 1f);
            var actionIdle = new Color(0.28f, 0.30f, 0.34f, 1f);
            var closeIdle = new Color(0.48f, 0.26f, 0.26f, 1f);
            var closePressed = new Color(0.32f, 0.16f, 0.16f, 1f);

            // 字号应略降：WindowTitle > Tab > SectionTitle > Body。
            // Body 与 Action 同级，不超过 SectionTitle；Close 字号不超过 Tab，Action 字号不超过 SectionTitle。
            // Tab、TabSelected、Action、ActionSelected 与 TitleRowHeight 同高。TabSelected 只换底色，尺寸字号跟 Tab；ActionSelected 跟 Action。
            // Close 高度不超过 TitleRowHeight。WindowTitle 字号与 TextLinePadding 之和不超过 TitleRowHeight。
            // 间距小于所分隔控件的高度：TabSpacing < Tab，RowSpacing < Action，PanelSpacing 与 SectionSpacing 同理。
            return new GmDebugSkin
            {
                WindowTitle = MakeText(36, label, TextAnchor.MiddleLeft),
                SectionTitle = MakeText(28, label, TextAnchor.MiddleLeft),
                Body = MakeText(26, body, TextAnchor.MiddleLeft),
                Tab = MakeButton(60f, 0f, true, 30, label, tabIdle, pressed),
                TabSelected = MakeButton(60f, 0f, true, 30, label, green, greenPressed),
                Close = MakeButton(48f, 120f, false, 26, label, closeIdle, closePressed),
                Action = MakeButton(60f, 0f, true, 26, label, actionIdle, pressed),
                ActionSelected = MakeButton(60f, 0f, true, 26, label, green, greenPressed),
                Panel = new Color(0.08f, 0.08f, 0.1f, 0.94f),
                Section = new Color(1f, 1f, 1f, 0.06f),
                PanelPadding = 16,
                PanelSpacing = 16,
                SectionPadding = 16,
                SectionSpacing = 12,
                TabSpacing = 18,
                RowSpacing = 12,
                TextLinePadding = 16,
                TitleRowHeight = 60f,
            };
        }

        static GmTextStyle MakeText(int fontSize, Color color, TextAnchor alignment)
        {
            return new GmTextStyle
            {
                FontSize = fontSize,
                Color = color,
                Alignment = alignment,
            };
        }

        // 选中态底色跟普通底色，避免 Selectable 把字色冲淡
        static GmButtonStyle MakeButton(
            float height,
            float preferredWidth,
            bool flexibleWidth,
            int fontSize,
            Color label,
            Color normal,
            Color pressed)
        {
            return new GmButtonStyle
            {
                Height = height,
                PreferredWidth = preferredWidth,
                FlexibleWidth = flexibleWidth,
                Label = MakeText(fontSize, label, TextAnchor.MiddleCenter),
                Normal = normal,
                Pressed = pressed,
                Selected = normal,
            };
        }
    }
}
