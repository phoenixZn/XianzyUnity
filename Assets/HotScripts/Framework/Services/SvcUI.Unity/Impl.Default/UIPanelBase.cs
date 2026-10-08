using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 全屏/窗口 Panel：自带 Canvas + Raycaster，Hide 时销毁 GameObject。
    /// </summary>
    public class UIPanelBase : UIBase
    {
        public UILayerId layerId
        {
            get => _layerId;
            set => _layerId = value;
        }

        public Canvas canvas => _canvas;
        public GraphicRaycaster raycaster => _raycaster;

        [SerializeField]
        UILayerId _layerId;

        Canvas _canvas;
        GraphicRaycaster _raycaster;

        string _uiName = "";
        public string UIName => _uiName;

        bool _isShowComplete;
        public bool IsShowComplete => _isShowComplete;

        bool _isActive;

        object[] mArgs;

        DictionaryList<string, UIViewBase> _needReverseView; // Hide 时收集带 scaleRoot 的子 View

        public override void Initialize()
        {
            _canvas = gameObject.AddComponent<Canvas>();
            _raycaster = gameObject.AddComponent<GraphicRaycaster>();
            base.Initialize();
        }

        /// <summary>
        /// 激活并 onShow；无 scaleRoot 时立刻视为开启动画结束。
        /// </summary>
        public void Show(string uiName, object[] args = null)
        {
            _uiName = uiName;
            _isActive = true;
            _isShow = true;

            if (args == null && mArgs != null)
                args = mArgs;
            else
                mArgs = args;

            gameObject.SetActive(true);
            onShow(args);
            if (scaleRoot == null)
            {
                _isShowComplete = true;
                OnShowScaleComplete();
            }
        }

        public virtual void OnShowScaleComplete()
        {
        }

        /// <summary>
        /// 从层列表移除，播关闭缩放后 Destroy。
        /// </summary>
        public void Hide(bool hideAll = false)
        {
            _isActive = false;
            _isShowComplete = false;
            mOwnerMng.Remove(this, hideAll);
            if (UIViews == null)
            {
                ViewHideInternal();
                return;
            }

            CollectNeedReverse(UIViews.ValueList);
            if (_needReverseView == null)
            {
                ViewHideInternal();
                return;
            }

            for (int i = 0; i < _needReverseView.ValueList.Count; i++)
                _needReverseView.ValueList[i].DoScaleReverse(CheckViewsReverseOver);
        }

        /// <summary>
        /// 被新 Panel 顶开时仅 SetActive(false)，不销毁，便于栈恢复。
        /// </summary>
        public void HideByOpen()
        {
            if (UIViews != null && UIViews.Count != 0)
            {
                for (int i = 0; i < UIViews.Count; i++)
                    UIViews.ValueList[i].Hide();
            }

            if (UIExtraArgsViews != null && UIExtraArgsViews.Count != 0)
            {
                for (int i = 0; i < UIExtraArgsViews.Count; i++)
                    UIExtraArgsViews.ValueList[i].Hide();
            }

            _isShow = false;
            onHide();
            gameObject.SetActive(false);
        }

        void CheckViewsReverseOver(string viewName)
        {
            _needReverseView.Remove(viewName);
            if (_needReverseView.Count == 0)
                ViewHideInternal();
        }

        public void CollectNeedReverse(List<UIViewBase> views)
        {
            for (int i = 0; i < views.Count; i++)
            {
                if (views[i].scaleRoot != null)
                {
                    if (_needReverseView == null)
                        _needReverseView = new DictionaryList<string, UIViewBase>();
                    if (!_needReverseView.TryGetValue(views[i].gameObject.name, out _))
                        _needReverseView.Add(views[i].gameObject.name, views[i]);
                }

                if (views[i].UIViews != null && views[i].UIViews.Count != 0)
                    CollectNeedReverse(views[i].UIViews.ValueList);
            }
        }

        void ViewHideInternal()
        {
            if (UIViews != null && UIViews.Count != 0)
            {
                for (int i = 0; i < UIViews.Count; i++)
                    UIViews.ValueList[i].Hide();
            }

            if (UIExtraArgsViews != null && UIExtraArgsViews.Count != 0)
            {
                for (int i = 0; i < UIExtraArgsViews.Count; i++)
                    UIExtraArgsViews.ValueList[i].Hide();
            }

            InternalHide();
        }

        void InternalHide()
        {
            if (scaleRoot != null)
            {
                DoScaleReverse(PanelInternalHide);
                return;
            }

            if (gameObject == null)
            {
                G.LogError("[UIPanelBase] Hide while gameObject is null");
                return;
            }

            PanelInternalHide(gameObject.name);
        }

        void PanelInternalHide(string panelName)
        {
            if (gameObject == null || panelName != gameObject.name)
                return;

            _isShow = false;
            onHide();
            OnDestroy();
            Object.Destroy(gameObject);
            mArgs = null;
        }

        public bool CheckPanelHasNeedDoScale()
        {
            bool panelNeedDoScale = scaleRoot != null;
            if (UIViews == null || UIViews.Count == 0)
                return panelNeedDoScale;

            for (int i = 0; i < UIViews.Count; i++)
                panelNeedDoScale |= UIViews.ValueList[i].scaleRoot != null;
            return panelNeedDoScale;
        }

        public override void onUpdate()
        {
            base.onUpdate();
            if (scaleRoot == null)
                return;
            if (!CheckPanelScaleOver())
                return;
            if (!_isActive)
                return;

            _isShowComplete = true;
            OnShowScaleComplete();
        }

        public bool CheckPanelScaleOver()
        {
            bool scaleOver = false;
            if (scaleRoot != null)
                scaleOver |= IsOpenScaleOver;

            if (UIViews == null || UIViews.Count == 0)
                return scaleOver;

            for (int i = 0; i < UIViews.Count; i++)
            {
                if (UIViews.ValueList[i].scaleRoot != null)
                    scaleOver |= UIViews.ValueList[i].IsOpenScaleOver;
            }

            return scaleOver;
        }

        public override void onHide()
        {
            base.onHide();
            _isShowComplete = false;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            _isShowComplete = false;
        }

        /// <summary>
        /// 把子 Canvas sortingOrder 叠加上当前 SortingOrder。
        /// </summary>
        public void SetSortingOrder()
        {
            var childCanvases = gameObject.GetComponentsInChildren<Canvas>();
            for (int i = 0; i < childCanvases.Length; i++)
                childCanvases[i].sortingOrder += SortingOrder;
        }

        /// <summary>
        /// 先减去旧 SortingOrder 再叠新值，并同步子 View 的 Canvas。
        /// </summary>
        public void SpecifySortingOrder(int sortingOrder)
        {
            var childCanvases = gameObject.GetComponentsInChildren<Canvas>();
            for (int i = 0; i < childCanvases.Length; i++)
            {
                if (childCanvases[i].sortingOrder >= SortingOrder)
                    childCanvases[i].sortingOrder -= SortingOrder;
                childCanvases[i].sortingOrder += sortingOrder;
            }

            SortingOrder = sortingOrder;

            if (UIViews == null)
                return;

            for (int i = 0; i < UIViews.ValueList.Count; i++)
            {
                var viewCanvases = UIViews.ValueList[i].gameObject.GetComponentsInChildren<Canvas>();
                for (int j = 0; j < viewCanvases.Length; j++)
                {
                    if (viewCanvases[j].sortingOrder >= SortingOrder)
                        viewCanvases[j].sortingOrder -= SortingOrder;
                    viewCanvases[j].sortingOrder += sortingOrder;
                }
            }
        }

        public void ClearArgs()
        {
            mArgs = null;
        }

        public void SetArgs(params object[] objs)
        {
            mArgs = objs;
        }
    }
}
