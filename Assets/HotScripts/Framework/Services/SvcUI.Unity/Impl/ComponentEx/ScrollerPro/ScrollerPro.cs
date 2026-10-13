using System;
using EnhancedUI.EnhancedScroller;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using static EnhancedUI.EnhancedScroller.EnhancedScroller;

namespace Xease.UI
{
    /// <summary>
    /// 垂直列表一行内的水平对齐。
    /// </summary>
    public enum PivotHorizontal
    {
        [LabelText("左对齐")]
        Left,
        [LabelText("单个item居中对齐")]
        Center, // 按每组固定个数居中
        [LabelText("居中对齐")]
        Middle, // 按当前真实个数居中
        [LabelText("右对齐")]
        Right,
    }

    /// <summary>
    /// 水平列表一列内的垂直对齐。
    /// </summary>
    public enum PivotVerticle
    {
        [LabelText("上对齐")]
        Top,
        [LabelText("单个item居中对齐")]
        Center,
        [LabelText("居中对齐")]
        Middle,
        [LabelText("下对齐")]
        Bottom,
    }

    /// <summary>
    /// EnhancedScroller 的格子封装：一组 cell 对应若干数据项，并转发对象池回调。
    /// </summary>
    public class ScrollerPro : EnhancedScroller, IEnhancedScrollerDelegate
    {
        public Action<GroupBase, int, int> GetObjectHandler; // 显示一组中的某一项
        public Action<int> ReturnObjectHandler; // 回收数据索引
        public Action<int> ProvideDataHandler; // 刷新数据索引
        public Func<int, int> GetGroupSizeHandler; // 按组索引返回主轴尺寸
        public Func<int> GetDataCountHandler; // 数据总数

        [ShowIf("@this.scrollDirection == ScrollDirectionEnum.Vertical")]
        public PivotHorizontal pivotHorizontal; // 垂直列表的行内水平对齐

        [ShowIf("@this.scrollDirection == ScrollDirectionEnum.Horizontal")]
        public PivotVerticle pivotVertical; // 水平列表的列内垂直对齐

        [LabelText("是否需要滚动")]
        public bool needScroller = true; // 为假时关掉 ScrollRect 的双向滚动

        public GroupBase groupPrefab; // 作为 cell 模板的组
        public bool isGrid; // 为真时一组内排多个 item
        bool _delegateBound; // Delegate 与滚动开关是否已绑定

        public EnhancedScroller Scroller => this;

        public int NumberOfCellsPerRow => groupPrefab.gridCount; // 一组里的 item 数

        /// <summary>
        /// 绑定委托，并按 needScroller 开关滚动。不可做成 Awake：会挡住基类 private Awake，Instantiate 时 OnEnable 空引用。
        /// </summary>
        public void Initialize()
        {
            if (_delegateBound)
                return;

            _delegateBound = true;
            Delegate = this;
            cellViewVisibilityChanged = CellViewVisibilityChanged;
            cellViewInstantiated = CellViewInstantiated;
            SetScroll(needScroller);
        }

        /// <summary>
        /// 开关滚动。开启时方向跟 scrollDirection 走。
        /// </summary>
        public void SetScroll(bool enable)
        {
            needScroller = enable;
            var scrollRect = GetComponent<ScrollRect>();
            if (scrollRect == null)
                return;

            if (enable)
            {
                scrollRect.horizontal = scrollDirection == ScrollDirectionEnum.Horizontal;
                scrollRect.vertical = scrollDirection == ScrollDirectionEnum.Vertical;
            }
            else
            {
                scrollRect.horizontal = false;
                scrollRect.vertical = false;
            }
        }

        /// <summary>
        /// 取出一组 cell，并写入该组覆盖的数据区间。
        /// </summary>
        public EnhancedScrollerCellView GetCellView(EnhancedScroller scroller, int dataIndex, int cellIndex)
        {
            var group = scroller.GetCellView(groupPrefab) as GroupBase;
            var perRow = NumberOfCellsPerRow;
            var start = dataIndex * perRow;
            group.name = "Group " + start + " to " + (start + perRow - 1);
            group.SetGroup(dataIndex / perRow, start, start + perRow - 1);
            return group;
        }

        /// <summary>
        /// 该组在滚动主轴上的尺寸。没有回调时为 0。
        /// </summary>
        public float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
        {
            if (GetGroupSizeHandler == null)
                return 0;

            return GetGroupSizeHandler(dataIndex);
        }

        /// <summary>
        /// 按数据总数换算组数。
        /// </summary>
        public int GetNumberOfCells(EnhancedScroller scroller)
        {
            if (GetDataCountHandler == null)
                return 0;

            return Mathf.CeilToInt((float)GetDataCountHandler() / NumberOfCellsPerRow);
        }

        /// <summary>
        /// cell 可见性变化时刷新组内 item。
        /// </summary>
        public void CellViewVisibilityChanged(EnhancedScrollerCellView cellView)
        {
            var group = cellView as GroupBase;
            group.RefreshCellView();
        }

        /// <summary>
        /// 实例化 cell 后激活节点。
        /// </summary>
        public void CellViewInstantiated(EnhancedScroller scroller, EnhancedScrollerCellView cellView)
        {
            cellView.gameObject.SetActive(true);
        }
    }
}
