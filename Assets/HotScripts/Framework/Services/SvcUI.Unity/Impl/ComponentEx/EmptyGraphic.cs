using UnityEngine;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 不绘制网格的 Graphic，只用来接收射线。
    /// </summary>
    public class EmptyGraphic : MaskableGraphic
    {
        /// <summary>
        /// 清空顶点，不产生可见网格。
        /// </summary>
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
        }
    }
}
