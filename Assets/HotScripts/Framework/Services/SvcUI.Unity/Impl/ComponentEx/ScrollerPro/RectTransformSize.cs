using UnityEngine;

namespace Xease.UI
{
    /// <summary>
    /// 取 RectTransform 当前矩形尺寸，供格子列表计算每组个数。
    /// </summary>
    public static class RectTransformSize
    {
        /// <summary>
        /// 返回 rect.size，而不是 sizeDelta 字段。
        /// </summary>
        public static Vector2 SizeDelta(this RectTransform rectTransform)
        {
            return rectTransform.rect.size;
        }
    }
}
