using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Xease.UI
{
    /// <summary>
    /// 遮罩把点击、按下和拖拽转发给 TargetObj 里的对象。
    /// 遮罩自身不要再收这些事件，否则会递归点到自己。
    /// </summary>
    public class PanerateMask : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler
    {
        readonly List<RaycastResult> _rawRaycastResults = new List<RaycastResult>();

        public List<GameObject> TargetObj; // 允许被穿透命中的对象

        /// <summary>
        /// 把点击转给命中的目标。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            Dispatch(eventData, target =>
                ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerClickHandler));
        }

        /// <summary>
        /// 把按下和抬起转给命中的目标。
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            Dispatch(eventData, target =>
            {
                ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerDownHandler);
                ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.pointerUpHandler);
            });
        }

        /// <summary>
        /// 把拖拽转给命中的目标。
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            Dispatch(eventData, target =>
                ExecuteEvents.ExecuteHierarchy(target, eventData, ExecuteEvents.dragHandler));
        }

        // 跳过 IgnoreEventRaycast，避免遮罩再次命中自己
        void Dispatch(PointerEventData eventData, Action<GameObject> execute)
        {
            if (EventSystem.current == null || TargetObj == null)
                return;

            _rawRaycastResults.Clear();
            EventSystem.current.RaycastAll(eventData, _rawRaycastResults);
            foreach (var result in _rawRaycastResults)
            {
                if (result.gameObject.GetComponent<IgnoreEventRaycast>())
                    continue;

                if (TargetObj.Count == 0)
                    return;

                if (TargetObj.Contains(result.gameObject))
                    execute(result.gameObject);
            }
        }
    }
    
    /// <summary>
    /// 穿透遮罩射线时跳过挂了本组件的节点，避免遮罩再次点到自己。
    /// </summary>
    public class IgnoreEventRaycast : MonoBehaviour
    {
    }
}
