using UnityEngine;

namespace Xease.UI
{
    /// <summary>
    /// 嵌套 View：挂到父节点，Show/Hide 以 SetActive 为主，不走 Panel 栈。
    /// </summary>
    public abstract class UIViewBase : UIBase
    {
        Transform _parent; // Initialize 时挂接的父节点

        public override void Initialize()
        {
            base.Initialize();
            Transform selfTrans = gameObject.transform;
            selfTrans.SetParent(_parent, false);
            selfTrans.localScale = Vector3.one;
            Show(null);
        }

        public void SetParent(Transform parent)
        {
            _parent = parent;
        }

        /// <summary>
        /// 激活 GameObject 并 onShow。
        /// </summary>
        public void Show(object[] args)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            _isShow = true;
            onShow(args);
        }

        /// <summary>
        /// 关闭 GameObject 并 onHide。
        /// </summary>
        public void Hide()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);

            _isShow = false;
            onHide();
        }

        /// <summary>
        /// 只走 onShow，不改 active。
        /// </summary>
        public void ShowWithoutActive()
        {
            _isShow = true;
            onShow(null);
        }

        /// <summary>
        /// 只走 onHide，不改 active。
        /// </summary>
        public void HideWithoutInactive()
        {
            _isShow = false;
            onHide();
        }
    }
}
