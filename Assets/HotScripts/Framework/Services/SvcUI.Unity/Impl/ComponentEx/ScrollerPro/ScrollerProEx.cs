using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 嵌套滚动：本轴不可滚时，把拖拽交给父级 ScrollRect。
    /// </summary>
    public class ScrollerProEx : ScrollRect
    {
        bool _routeToParent; // 当前拖拽是否交给父级

        void DoForParents<T>(Action<T> action) where T : IEventSystemHandler
        {
            var parent = transform.parent;
            while (parent != null)
            {
                foreach (var component in parent.GetComponents<Component>())
                {
                    if (component is T handler)
                        action(handler);
                }

                parent = parent.parent;
            }
        }

        /// <summary>
        /// 初始化拖拽时同步通知父级。
        /// </summary>
        public override void OnInitializePotentialDrag(PointerEventData eventData)
        {
            DoForParents<IInitializePotentialDragHandler>(parent => parent.OnInitializePotentialDrag(eventData));
            base.OnInitializePotentialDrag(eventData);
        }

        /// <summary>
        /// 拖拽。交给父级或自己处理。
        /// </summary>
        public override void OnDrag(PointerEventData eventData)
        {
            if (_routeToParent)
                DoForParents<IDragHandler>(parent => parent.OnDrag(eventData));
            else
                base.OnDrag(eventData);
        }

        /// <summary>
        /// 开始拖拽。位移主轴与本控件可滚方向不一致时转给父级。
        /// </summary>
        public override void OnBeginDrag(PointerEventData eventData)
        {
            if (!horizontal && Math.Abs(eventData.delta.x) > Math.Abs(eventData.delta.y))
                _routeToParent = true;
            else if (!vertical && Math.Abs(eventData.delta.x) < Math.Abs(eventData.delta.y))
                _routeToParent = true;
            else
                _routeToParent = false;

            if (_routeToParent)
                DoForParents<IBeginDragHandler>(parent => parent.OnBeginDrag(eventData));
            else
                base.OnBeginDrag(eventData);
        }

        /// <summary>
        /// 结束拖拽，并清掉转父级标记。
        /// </summary>
        public override void OnEndDrag(PointerEventData eventData)
        {
            if (_routeToParent)
                DoForParents<IEndDragHandler>(parent => parent.OnEndDrag(eventData));
            else
                base.OnEndDrag(eventData);

            _routeToParent = false;
        }
    }
}
