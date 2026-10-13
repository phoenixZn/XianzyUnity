using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 横向 ScrollRect 松手后吸附到预设锚点。
    /// </summary>
    public class MainScrollEx : MonoBehaviour, IBeginDragHandler, IEndDragHandler
    {
        const float SnapMoveSpeed = 5000f; // 吸附时每秒位移，anchoredPosition 单位

        public ScrollRect ScrollRect;
        public GameObject Content;
        public bool FlickSnap;
        public float Speed; // Awake 时写入 ScrollRect.velocity 的速率
        public Action<int> OnScroll2Index; // 吸附完成后的页索引

        // 横向吸附点的 anchoredPosition.x
        readonly float[] _snapPositions =
        {
            0, -1080, -1080 * 2, -1080 * 3, -1080 * 4
        };

        RectTransform _contentRectTrans;
        bool _dragging;
        bool _flicking;
        Coroutine _coroutine;

        /// <summary>
        /// 绑定 ScrollRect 与 Content，并写入初始速度。
        /// </summary>
        public void Awake()
        {
            if (ScrollRect == null)
                ScrollRect = GetComponent<ScrollRect>();

            if (ScrollRect == null)
            {
                G.LogError("[MainScrollEx] ScrollRect is missing");
                return;
            }

            if (Content == null)
            {
                var contentTrans = ScrollRect.transform.Find("Content");
                if (contentTrans == null)
                {
                    G.LogError("[MainScrollEx] Content is missing");
                    return;
                }

                Content = contentTrans.gameObject;
            }

            _contentRectTrans = Content.transform as RectTransform;
            var xSpeed = ScrollRect.horizontal ? Speed : 0f;
            var ySpeed = ScrollRect.vertical ? Speed : 0f;
            ScrollRect.velocity = new Vector2(xSpeed, ySpeed);
            _flicking = false;
        }

        void LateUpdate()
        {
            if (Content == null || ScrollRect == null || _contentRectTrans == null)
                return;

            if (!FlickSnap || _dragging || !_flicking)
                return;

            var nearest = float.MaxValue;
            var nearestIndex = -1;
            for (var i = 0; i < _snapPositions.Length; i++)
            {
                var distance = Mathf.Abs(_contentRectTrans.anchoredPosition.x - _snapPositions[i]);
                if (distance >= nearest)
                    continue;

                nearest = distance;
                nearestIndex = i;
            }

            if (nearestIndex == -1)
                return;

            _flicking = false;
            _coroutine = StartCoroutine(ScrollTo(nearestIndex, SnapMoveSpeed));
        }

        /// <summary>
        /// 立即落到指定吸附点，不播移动。
        /// </summary>
        public void StartByIndex(int index)
        {
            if (_contentRectTrans == null)
                return;

            _contentRectTrans.anchoredPosition = new Vector2(_snapPositions[index], _contentRectTrans.anchoredPosition.y);
        }

        /// <summary>
        /// 以吸附速度移动到指定页。
        /// </summary>
        public void ScrollTo(int index)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _coroutine = StartCoroutine(ScrollTo(index, SnapMoveSpeed));
        }

        /// <summary>
        /// 开始拖拽时打断吸附。
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);

            _dragging = true;
        }

        /// <summary>
        /// 结束拖拽后允许下一帧吸附。
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            _dragging = false;
            _flicking = true;
        }

        IEnumerator ScrollTo(int index, float speed)
        {
            var targetX = _snapPositions[index];
            while (Mathf.Abs(_contentRectTrans.anchoredPosition.x - targetX) > 0.1f)
            {
                var displacement = speed * Time.deltaTime;
                var dir = targetX < _contentRectTrans.anchoredPosition.x ? -1 : 1;
                var newX = _contentRectTrans.anchoredPosition.x + dir * displacement;
                if ((newX < targetX && dir == -1) || (newX > targetX && dir == 1))
                    newX = targetX;

                _contentRectTrans.anchoredPosition = new Vector2(newX, _contentRectTrans.anchoredPosition.y);
                yield return null;
            }

            _contentRectTrans.anchoredPosition = new Vector2(targetX, _contentRectTrans.anchoredPosition.y);
            OnScroll2Index?.Invoke(index);
        }

        void OnDestroy()
        {
            if (_coroutine != null)
                StopCoroutine(_coroutine);
        }
    }
}
