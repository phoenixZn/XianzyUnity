using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 按下为 true、抬起为 false 的按钮事件。
    /// </summary>
    [Serializable]
    public class SwitchClickEvent : UnityEvent<bool>
    {
    }

    /// <summary>
    /// 在按下/抬起时把按压状态传给 switchClick。
    /// </summary>
    public class SwitchClickButton : Button
    {
        [SerializeField]
        public SwitchClickEvent switchClick;

        /// <summary>
        /// 按下并通知 true。
        /// </summary>
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            switchClick?.Invoke(true);
        }

        /// <summary>
        /// 抬起并通知 false。
        /// </summary>
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            switchClick?.Invoke(false);
        }

        /// <summary>
        /// 指针离开时走按钮默认处理。
        /// </summary>
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
        }
    }
}
