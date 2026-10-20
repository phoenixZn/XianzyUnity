using UnityEngine;
using UnityEngine.UI;

namespace Xease
{
    public partial class GmStaticAction : IGmCanvasBlock
    {
        //////////////////////////////////////////////////////////////////////////
        /// IGmCanvasBlock:

        /// <summary>在滚动内容中按登记顺序加按钮，点击后调用对应函数。</summary>
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
            for (int i = 0; i < _entries.Count; i++)
            {
                // 先抄到局部变量再绑定，避免点击时拿到循环末项
                var action = _entries[i].Action;
                GmRuntimeUi.CreateButton(_entries[i].Name, section, () => action());
            }
        }
    }
}
