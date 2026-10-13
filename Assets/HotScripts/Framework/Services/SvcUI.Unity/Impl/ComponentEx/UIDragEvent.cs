using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Xease.UI
{
    /// <summary>
    /// 把按下、拖拽、抬起的屏幕坐标转成回调。
    /// </summary>
    public class UIDragEvent : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public Action<Vector2> OnUIStartDrag; // 按下时的屏幕坐标
        public Action<Vector2> OnUIDrag; // 拖拽中的屏幕坐标
        public Action<Vector2> OnUIEndDrag; // 抬起时的屏幕坐标

        /// <summary>
        /// 按下，参数为屏幕坐标。
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            OnUIStartDrag?.Invoke(eventData.position);
        }

        /// <summary>
        /// 拖拽中，参数为屏幕坐标。
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            OnUIDrag?.Invoke(eventData.position);
        }

        /// <summary>
        /// 抬起，参数为屏幕坐标。
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            OnUIEndDrag?.Invoke(eventData.position);
        }

        /// <summary>
        /// 清空三段拖拽回调。
        /// </summary>
        public void ResetDragEvent()
        {
            OnUIStartDrag = null;
            OnUIDrag = null;
            OnUIEndDrag = null;
        }
    }
}
