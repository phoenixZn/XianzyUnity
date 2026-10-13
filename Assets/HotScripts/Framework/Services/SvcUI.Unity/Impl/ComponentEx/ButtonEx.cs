using System;
using LitMotion;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Xease.UI
{
    /// <summary>
    /// 按钮按下/抬起缩放，点击时按事件名播放音效。
    /// </summary>
    public class ButtonEx : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        const float ReleaseMidScale = 0.9f; // 抬起中间帧相对基准缩放的比例

        public bool scalable = true; // 是否播放按下缩放
        public Vector3 downTo = Vector3.one; // 按下目标缩放
        public Vector3 upMidTo = new Vector3(0.9f, 0.9f, 1f); // 抬起中间缩放
        public Vector3 upTo = Vector3.one; // 抬起结束缩放
        public float Duration = 0.1f; // 单段缩放时长，秒
        public bool hasSound = true; // 点击是否播音效
        public string soundName = "ui_click"; // 默认点击事件名
        public Func<string> SetSound; // 非空时用其返回值作为事件名
        public Action onPointDown;
        public Action onPointUp;
        public Action onPointEnter;
        public Action onPointExit;
        public bool scaleConfig; // 为真时按当前 localScale 计算三段目标

        MotionHandle? _pressMotion;
        MotionHandle? _releaseMidMotion;
        MotionHandle? _releaseEndMotion;

        /// <summary>
        /// 按 scaleConfig 写入三段缩放目标。
        /// </summary>
        public void Awake()
        {
            if (scaleConfig)
            {
                var scale = transform.localScale.x;
                downTo = new Vector3(scale, scale, 1f);
                upMidTo = new Vector3(scale * ReleaseMidScale, scale * ReleaseMidScale, 1f);
                upTo = new Vector3(scale, scale, 1f);
            }
            else
            {
                downTo = Vector3.one;
                upMidTo = new Vector3(ReleaseMidScale, ReleaseMidScale, 1f);
                upTo = Vector3.one;
            }
        }

        /// <summary>
        /// 按下回调，并缩放到 downTo。
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            onPointDown?.Invoke();
            if (!scalable || transform == null)
                return;

            Cancel(ref _pressMotion);
            _pressMotion = PlayScale(downTo, null);
        }

        /// <summary>
        /// 抬起回调：先到中间缩放，再回到 upTo。
        /// </summary>
        public void OnPointerUp(PointerEventData eventData)
        {
            onPointUp?.Invoke();
            if (!scalable || transform == null)
                return;

            Cancel(ref _releaseMidMotion);
            _releaseMidMotion = PlayScale(upMidTo, () =>
            {
                if (transform == null)
                    return;

                Cancel(ref _releaseEndMotion);
                _releaseEndMotion = PlayScale(upTo, null);
            });
        }

        /// <summary>
        /// 点击时播放音效。事件名优先取 SetSound。
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            hasSound = true;
            if (!hasSound)
                return;

            if (soundName == "UI_Click")
                soundName = "ui_click";

            var realSoundName = SetSound == null ? soundName : SetSound.Invoke();
            if (string.IsNullOrEmpty(realSoundName))
                return;

            G.Audio.Play(realSoundName, Vector3.zero);
        }

        /// <summary>
        /// 进入指针范围。
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            onPointEnter?.Invoke();
        }

        /// <summary>
        /// 离开指针范围。
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            onPointExit?.Invoke();
        }

        /// <summary>
        /// 停掉缩放并把 localScale 还原为单位缩放。
        /// </summary>
        public void Clear()
        {
            if (gameObject != null)
                gameObject.transform.localScale = Vector3.one;

            Cancel(ref _pressMotion);
            Cancel(ref _releaseMidMotion);
            Cancel(ref _releaseEndMotion);
        }

        void OnDestroy()
        {
            Clear();
        }

        MotionHandle PlayScale(Vector3 to, Action onComplete)
        {
            var motion = LMotion.Create(transform.localScale, to, Duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale);
            if (onComplete != null)
                motion = motion.WithOnComplete(onComplete);

            return motion.Bind(transform, static (value, target) =>
            {
                if (target != null)
                    target.localScale = value;
            });
        }

        static void Cancel(ref MotionHandle? handle)
        {
            if (!handle.HasValue)
                return;

            handle.Value.TryCancel();
            handle = null;
        }
    }
}
