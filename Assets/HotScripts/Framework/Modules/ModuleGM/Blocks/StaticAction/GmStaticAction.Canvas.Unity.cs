using UnityEngine;

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

            var skin = GmDebugSkin.Current;
            var section = GmRuntimeUi.CreateSection(Id, content);
            GmRuntimeUi.CreateText("Title", Title, section, skin.SectionTitle, true);
            for (int i = 0; i < _entries.Count; i++)
            {
                // 先抄到局部变量再绑定，避免点击时拿到循环末项
                var action = _entries[i].Action;
                GmRuntimeUi.CreateButton(_entries[i].Name, section, () => action(), skin.Action);
            }
        }
    }
}
