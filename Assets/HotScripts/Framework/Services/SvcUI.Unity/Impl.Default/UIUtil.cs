using UnityEngine;

namespace Xease.UI
{
    /// <summary>
    /// 在已有节点上绑定/显隐 UIViewBase 的辅助方法。
    /// </summary>
    public static class UIUtil
    {
        /// <summary>
        /// Hide + OnDestroy，并把引用置空。
        /// </summary>
        public static void DestroyWidget<T>(ref T widget) where T : UIViewBase
        {
            if (widget == null)
                return;

            widget.Hide();
            widget.OnDestroy();
            widget = null;
        }

        /// <summary>
        /// 在 root 下按节点名查找并绑定已存在的 View 节点（不加载预制体）。
        /// </summary>
        public static void CreateWidget<T>(GameObject root, string widgetPrefabName, ref T widget)
            where T : UIViewBase, new()
        {
            if (root == null)
            {
                G.LogError($"[UIUtil] CreateWidget failed, root is null. name={widgetPrefabName}");
                return;
            }

            var widgetRewardView = root.transform.Find(widgetPrefabName);
            if (widgetRewardView == null)
            {
                G.LogError($"[UIUtil] node not found: {widgetPrefabName}");
                return;
            }

            widget = new T();
            widget.gameObject = widgetRewardView.gameObject;
            widget.BindUI();
            widget.onCreate();
        }

        /// <summary>
        /// 先 Hide 再 Show，用于刷新已有 widget。
        /// </summary>
        public static void ShowWidget(UIViewBase widget, object[] objs = null)
        {
            if (widget == null)
                return;

            widget.Hide();
            widget.Show(objs);
        }
    }
}
