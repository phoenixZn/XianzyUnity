using EnhancedUI.EnhancedScroller;
using UnityEngine;
using static EnhancedUI.EnhancedScroller.EnhancedScroller;

namespace Xease.UI
{
    /// <summary>
    /// 一组滚动 cell。一组里按 gridCount 摆放数据项，并在可见性变化时取还对象。
    /// </summary>
    public class GroupBase : EnhancedScrollerCellView
    {
        public int groupIndex; // 组序号
        public int groupStart; // 组内第一个数据索引
        public int groupEnd; // 组内最后一个数据索引（含）
        public int gridCount; // 本组可放的 item 数
        public Vector3 groupSize; // 组矩形尺寸
        public ScrollerPro scrollerPro;

        /// <summary>
        /// 记录所属列表，并按格子或单列计算 gridCount。
        /// </summary>
        public void InitGroup(ScrollerPro scrollerPro, Transform itemPrefab)
        {
            this.scrollerPro = scrollerPro;
            var rect = transform as RectTransform;
            groupSize = SizeDelta(rect);
            if (scrollerPro.isGrid)
            {
                var itemRect = itemPrefab as RectTransform;
                var itemRectSize = SizeDelta(itemRect);
                if (scrollerPro.scrollDirection == ScrollDirectionEnum.Vertical)
                    gridCount = (int)(groupSize.x / (itemRectSize.x + scrollerPro.Scroller.spacing));
                else
                    gridCount = (int)(groupSize.y / (itemRectSize.y + scrollerPro.Scroller.spacing));
            }
            else
            {
                gridCount = 1;
            }

            cellIdentifier = "GroupBase";
        }

        /// <summary>
        /// 写入本组对应的数据区间。
        /// </summary>
        public void SetGroup(int groupIndex, int groupStart, int groupEnd)
        {
            this.groupIndex = groupIndex;
            this.groupStart = groupStart;
            this.groupEnd = groupEnd;
        }

        /// <summary>
        /// 可见时向列表要对象并填数据，不可见时归还。
        /// </summary>
        public override void RefreshCellView()
        {
            var max = scrollerPro.GetDataCountHandler();
            var length = 0;
            for (var i = 0; i < gridCount; i++)
            {
                if (groupStart + i >= max)
                    continue;

                length++;
            }

            for (var i = 0; i < length; i++)
            {
                if (active)
                {
                    scrollerPro.GetObjectHandler?.Invoke(this, groupStart + i, groupStart + length - 1);
                    scrollerPro.ProvideDataHandler?.Invoke(groupStart + i);
                }
                else
                {
                    scrollerPro.ReturnObjectHandler?.Invoke(groupStart + i);
                }
            }
        }
        
        /// <summary>
        /// 取 RectTransform 当前矩形尺寸，供格子列表计算每组个数。
        /// 返回 rect.size，而不是 sizeDelta 字段。
        /// </summary>
        public static Vector2 SizeDelta(RectTransform rectTransform)
        {
            return rectTransform.rect.size;
        }
    }

}
