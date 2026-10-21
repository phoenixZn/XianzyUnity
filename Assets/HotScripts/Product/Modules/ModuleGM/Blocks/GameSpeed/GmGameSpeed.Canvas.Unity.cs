using UnityEngine;
using UnityEngine.UI;

namespace Xease
{
    public partial class GmGameSpeed : IGmCanvasBlock
    {
        Text _currentLabel; // 展示 Current，不另存倍速
        Button[] _presetButtons; // 与 Presets 下标对齐

        //////////////////////////////////////////////////////////////////////////
        /// IGmCanvasBlock:

        /// <summary>在滚动内容中加一块预设按钮，点击后走 <see cref="TrySet"/>。</summary>
        public void AddToCanvas(RectTransform content)
        {
            if (content == null)
                return;

            var skin = GmDebugSkin.Current;
            var section = GmRuntimeUi.CreateSection(Id, content);
            GmRuntimeUi.CreateText("Title", Title, section, skin.SectionTitle, true);
            _currentLabel = GmRuntimeUi.CreateText("Current", string.Empty, section, skin.Body, true);

            var row = GmRuntimeUi.CreateRect("Presets", section);
            var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.spacing = skin.RowSpacing;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = true;
            rowLayout.childForceExpandHeight = true;
            var rowElement = row.gameObject.AddComponent<LayoutElement>();
            rowElement.preferredHeight = skin.Action.Height;
            rowElement.minHeight = skin.Action.Height;

            _presetButtons = new Button[Presets.Length];
            for (int i = 0; i < Presets.Length; i++)
            {
                var preset = Presets[i];
                _presetButtons[i] = GmRuntimeUi.CreateButton("x" + FormatSpeed(preset), row, () => TrySet(preset), skin.Action);
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

            var skin = GmDebugSkin.Current;
            for (int i = 0; i < _presetButtons.Length && i < Presets.Length; i++)
            {
                var on = IsCurrent(Presets[i]);
                GmRuntimeUi.ApplyButtonStyle(_presetButtons[i], on ? skin.ActionSelected : skin.Action);
            }
        }
    }
}
