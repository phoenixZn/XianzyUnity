using UnityEngine;
using UnityEngine.UI;

namespace Xease
{
    public partial class GmGameSpeed : IGmCanvasBlock
    {
        // 非当前档、当前档、按下时的按钮底色。只影响显示
        static readonly Color IdleTint = new Color(0.25f, 0.27f, 0.3f, 1f);
        static readonly Color ActiveTint = new Color(0.22f, 0.48f, 0.32f, 1f);
        static readonly Color PressedTint = new Color(0.15f, 0.16f, 0.18f, 1f);

        Text _currentLabel; // 展示 Current，不另存倍速
        Button[] _presetButtons; // 与 Presets 下标对齐

        //////////////////////////////////////////////////////////////////////////
        /// IGmCanvasBlock:

        /// <summary>在滚动内容中加一块预设按钮，点击后走 <see cref="TrySet"/>。</summary>
        public void AddToCanvas(RectTransform content)
        {
            if (content == null)
                return;

            var section = GmRuntimeUi.CreateRect(Id, content);
            var sectionImage = section.gameObject.AddComponent<Image>();
            sectionImage.sprite = GmRuntimeUi.WhiteSprite;
            sectionImage.color = new Color(1f, 1f, 1f, 0.06f);
            var sectionLayout = section.gameObject.AddComponent<VerticalLayoutGroup>();
            sectionLayout.padding = new RectOffset(12, 12, 12, 12);
            sectionLayout.spacing = 8;
            sectionLayout.childControlWidth = true;
            sectionLayout.childControlHeight = true;
            sectionLayout.childForceExpandWidth = true;
            sectionLayout.childForceExpandHeight = false;

            GmRuntimeUi.CreateText("Title", Title, section, GmRuntimeUi.BodyFontSize, true);
            _currentLabel = GmRuntimeUi.CreateText("Current", string.Empty, section, GmRuntimeUi.BodyFontSize, true);

            var row = GmRuntimeUi.CreateRect("Presets", section);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = 8;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            var rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = GmRuntimeUi.ButtonHeight;
            rowElement.minHeight = GmRuntimeUi.ButtonHeight;

            _presetButtons = new Button[Presets.Length];
            for (int i = 0; i < Presets.Length; i++)
            {
                var preset = Presets[i];
                _presetButtons[i] = GmRuntimeUi.CreateButton("x" + FormatSpeed(preset), row, () => TrySet(preset));
            }

            RefreshRow();
        }

        partial void OnSpeedApplied()
        {
            RefreshRow();
        }

        // 标签和高亮都从 Current / Presets 现算
        void RefreshRow()
        {
            if (_currentLabel == null)
                return;

            _currentLabel.text = "当前 " + FormatSpeed(Current);
            if (_presetButtons == null)
                return;

            for (int i = 0; i < _presetButtons.Length && i < Presets.Length; i++)
            {
                var button = _presetButtons[i];
                if (button == null)
                    continue;

                var colors = button.colors;
                var on = IsCurrent(Presets[i]);
                var tint = on ? ActiveTint : IdleTint;
                colors.normalColor = tint;
                colors.highlightedColor = tint;
                colors.selectedColor = tint;
                colors.pressedColor = PressedTint;
                colors.fadeDuration = 0f;
                button.colors = colors;
            }
        }
    }
}
