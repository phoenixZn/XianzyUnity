#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Xease.UI;

namespace Xease.UI
{
    /// <summary>
    /// 在 Hierarchy 里创建空白点击区和循环列表节点。
    /// </summary>
    public static class ComponentExMenus
    {
        /// <summary>
        /// 在当前选中节点下创建只接收射线的 EmptyGraphic。
        /// </summary>
        [MenuItem("GameObject/UI/空白点击组件")]
        public static void CreateEmptyGraphic()
        {
            if (Selection.activeTransform == null)
                return;

            var obj = new GameObject("EmptyGraphic");
            obj.AddComponent<CanvasRenderer>();
            var graphic = obj.AddComponent<EmptyGraphic>();
            graphic.raycastTarget = true;
            obj.transform.SetParent(Selection.activeTransform, false);
            Selection.activeTransform = obj.transform;
        }

        /// <summary>
        /// 创建垂直循环列表，含透明底和 item 子节点。
        /// </summary>
        [MenuItem("GameObject/UI/垂直滑动列表")]
        public static void CreateLoopVerticalScroll()
        {
            CreateLoopScroll(false, true, true);
        }

        /// <summary>
        /// 创建水平循环列表，含透明底和 item 子节点。
        /// </summary>
        [MenuItem("GameObject/UI/水平滑动列表")]
        public static void CreateLoopHorizontalScroll()
        {
            CreateLoopScroll(true, false, true);
        }

        /// <summary>
        /// 创建不滚动的循环列表。
        /// </summary>
        [MenuItem("GameObject/UI/不需要滑动的滑动列表")]
        public static void CreateNoneLoopScroll()
        {
            CreateLoopScroll(false, false, false);
        }

        static void CreateLoopScroll(bool horizontal, bool vertical, bool needScroller)
        {
            if (Selection.activeTransform == null)
                return;

            var obj = new GameObject("t_LoopScroll");
            var ex = obj.AddComponent<ScrollerProEx>();
            var pro = obj.AddComponent<ScrollerPro>();
            ex.horizontal = horizontal;
            ex.vertical = vertical;
            pro.needScroller = needScroller;
            obj.AddComponent<RectMask2D>();

            var bg = obj.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0);
            bg.raycastTarget = true;

            var item = new GameObject("item");
            item.AddComponent<RectTransform>();
            item.transform.SetParent(obj.transform, false);

            obj.transform.SetParent(Selection.activeTransform, false);
            Selection.activeTransform = obj.transform;
        }
    }

    /// <summary>
    /// 新建 Graphic 时默认不接收射线；新建 Button 时补上按下缩放。
    /// </summary>
    [InitializeOnLoad]
    public class ComponentAddListener
    {
        static ComponentAddListener()
        {
            ObjectFactory.componentWasAdded += ComponentWasAdded;
        }

        static void ComponentWasAdded(Component obj)
        {
            if (obj is MaskableGraphic graphic)
                graphic.raycastTarget = false;

            if (obj is Button)
                obj.gameObject.AddComponent<ButtonEx>();
        }
    }
}
#endif
