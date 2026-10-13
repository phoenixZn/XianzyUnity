using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Xease.UI
{
    /// <summary>
    /// 循环列表的一项。按节点名把子物体填进字段，点击走 UIEventListener。
    /// 归还引用池时走 OnRecycle。
    /// </summary>
    public class LoopScrollItem : IReference
    {
        public int index = -1; // 数据索引，未绑定为 -1
        public GroupBase groupBase;
        public int groupIndex => groupBase.groupIndex;
        public int groupStart => groupBase.groupStart;
        public GameObject gameObject;
        public ILoopScroll loopScroll;
        public GameObject selectGo; // 选中态节点，名字需与字段一致以便自动绑定
        public GameObject normalGo; // 未选中态节点

        protected UIBase OwnerUI; // 所属界面，Init 时写入

        /// <summary>
        /// 绑定列表、节点和所属 UI，并注册点击。
        /// </summary>
        public void Init(ILoopScroll loopScroll, GameObject gameObject, UIBase uiBase)
        {
            this.loopScroll = loopScroll;
            this.gameObject = gameObject;
            OwnerUI = uiBase;
            AutoReference(gameObject.transform, this);
            UIEventListener.OnClick(this.gameObject).AddListener(OnItemClick);
            OnInit();
        }

        /// <summary>
        /// 按字段名（忽略大小写）把同名子节点写进 UnityEngine.Object 字段。
        /// </summary>
        public void AutoReference(Transform transform, object obj)
        {
            var fieldInfoDict = new Dictionary<string, FieldInfo>();
            var fieldInfos = obj.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var objectType = typeof(Object);
            foreach (var item in fieldInfos)
            {
                if (item.FieldType.IsSubclassOf(objectType))
                    fieldInfoDict[item.Name.ToLower()] = item;
            }

            if (fieldInfoDict.Count == 0)
                return;

            BindNamedTransforms(transform, obj, fieldInfoDict);
        }

        /// <summary>
        /// 创建后的扩展点。
        /// </summary>
        public virtual void OnInit()
        {
        }

        /// <summary>
        /// 主动销毁扩展点。
        /// </summary>
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// 项显示时的扩展点。
        /// </summary>
        public virtual void OnShow()
        {
        }

        /// <summary>
        /// 项隐藏时的扩展点。
        /// </summary>
        public virtual void OnHide()
        {
        }

        /// <summary>
        /// 写入数据后按当前选中索引刷新选中态。
        /// </summary>
        public virtual void UpdateData(object obj)
        {
            OnItemSelect(loopScroll.CurSelect);
        }

        /// <summary>
        /// 点击：先选中再派发点击。
        /// </summary>
        public virtual void OnItemClick(GameObject obj)
        {
            loopScroll.OnItemSelect(index);
            loopScroll.OnItemClick(index);
        }

        /// <summary>
        /// 选中索引变化时刷新本项显隐。
        /// </summary>
        public virtual void OnItemSelect(int index)
        {
            UpdateSelectSpriteVisible(this.index == index);
        }

        /// <summary>
        /// 切换 selectGo / normalGo。
        /// </summary>
        public virtual void UpdateSelectSpriteVisible(bool visible)
        {
            if (selectGo != null && selectGo.activeSelf != visible)
                selectGo.SetActive(visible);

            if (normalGo != null && normalGo.activeSelf == visible)
                normalGo.SetActive(!visible);
        }

        /// <summary>
        /// 改本项尺寸并让列表重排。
        /// </summary>
        public void SetSize(Vector2 size)
        {
            loopScroll.SetSize(index, size);
        }

        /// <summary>
        /// 只改宽度。
        /// </summary>
        public void SetSizeX(int x)
        {
            loopScroll.SetSizeX(index, x);
        }

        /// <summary>
        /// 只改高度。
        /// </summary>
        public void SetSizeY(int y)
        {
            loopScroll.SetSizeY(index, y);
        }

        /// <summary>
        /// 创建扩展点，当前无额外工作。
        /// </summary>
        public void OnCreate()
        {
        }

        /// <summary>
        /// 归还引用池：隐藏并挂回列表节点，重置变换。
        /// </summary>
        public virtual void OnRecycle()
        {
            if (gameObject == null)
                return;

            var rect = gameObject.transform as RectTransform;
            gameObject.SetActive(false);
            if (groupBase == null || rect == null)
                return;

            rect.SetParent(groupBase.scrollerPro.transform);
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

        /// <summary>
        /// 销毁并清空一个子 View。
        /// </summary>
        protected void DestroyWidget<T>(ref T widget) where T : UIViewBase
        {
            UIUtil.DestroyWidget(ref widget);
        }

        /// <summary>
        /// 在 root 下按节点名绑定子 View。
        /// </summary>
        protected void CreateWidget<T>(GameObject root, string widgetPrefabName, ref T widget)
            where T : UIViewBase, new()
        {
            UIUtil.CreateWidget(root, widgetPrefabName, ref widget);
        }

        /// <summary>
        /// 重新显示已有子 View。
        /// </summary>
        protected void ShowWidget(UIViewBase widget, object[] objs = null)
        {
            UIUtil.ShowWidget(widget, objs);
        }

        static void BindNamedTransforms(Transform transform, object obj, Dictionary<string, FieldInfo> fieldInfoDict)
        {
            AssignField(transform, obj, fieldInfoDict);
            var children = transform.GetComponentsInChildren<Transform>(true);
            foreach (var item in children)
                AssignField(item, obj, fieldInfoDict);
        }

        static void AssignField(Transform transform, object obj, Dictionary<string, FieldInfo> fieldInfoDict)
        {
            var name = transform.name.ToLower();
            if (!fieldInfoDict.TryGetValue(name, out var field))
                return;

            if (field.FieldType == typeof(GameObject))
                field.SetValue(obj, transform.gameObject);
            else if (field.FieldType == typeof(Transform))
                field.SetValue(obj, transform);
            else
                field.SetValue(obj, transform.GetComponent(field.FieldType));
        }
    }
}
