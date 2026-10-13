using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 按模板子节点复制出固定数量的 UIViewBase。
    /// 第一个子节点是模板，真正的项从第二个开始。
    /// </summary>
    public class UIContainer
    {
        public const string Template = "Template"; // 模板子节点名

        public GameObject gameObject;
        public Transform transform => gameObject.transform;
        public GameObject Item;
        public int Count; // 当前数据个数

        readonly List<UIViewBase> _children = new List<UIViewBase>();
        readonly List<GameObject> _templateChildren = new List<GameObject>();

        public List<UIViewBase> Children => _children;

        /// <summary>
        /// 绑定容器根节点。
        /// </summary>
        public UIContainer(GameObject gameObject)
        {
            this.gameObject = gameObject;
        }

        /// <summary>
        /// 按数量从模板复制子 View。objs 与数量不一致时打错误日志，仍按现有数组显示。
        /// </summary>
        public void OnInit<T>(int count, object[] objs) where T : UIViewBase, new()
        {
            Count = count;
            var itemTrans = FindTemplate();
            if (itemTrans == null)
                return;

            for (var i = 0; i < Count; i++)
            {
                var obj = Object.Instantiate(itemTrans.gameObject, itemTrans.parent, false);
                obj.name = i.ToString();
                obj.SetActive(true);
                var view = new T
                {
                    gameObject = obj.transform.GetChild(0).gameObject
                };
                view.BindUI();
                view.onCreate();
                if (objs == null)
                {
                    G.LogError("[UIContainer] data is null");
                    view.onShow(new object[] { null });
                }
                else
                {
                    if (objs.Length != Count)
                        G.LogError("[UIContainer] data count does not match child count");

                    view.onShow(new object[] { objs[i] });
                }

                _children.Add(view);
            }
        }

        /// <summary>
        /// 隐藏已创建的子 View。
        /// </summary>
        public void OnHide()
        {
            for (var i = 0; i < _children.Count; i++)
                _children[i].onHide();
        }

        /// <summary>
        /// 销毁子 View 并清空列表。
        /// </summary>
        public void OnDestroy()
        {
            Count = 0;
            for (var i = 0; i < _children.Count; i++)
                _children[i].OnDestroy();

            _children.Clear();
        }

        /// <summary>
        /// 按新数据重建子项。localPos 非空时写到对应项的 localPosition。
        /// </summary>
        public void Refill<T>(object[] objs, List<Vector3> localPos = null) where T : UIViewBase, new()
        {
            var itemTemplate = FindTemplate();
            if (itemTemplate == null)
                return;

            if (objs == null || objs.Length < 1)
                return;

            if (itemTemplate.childCount < 1)
                return;

            Clear();
            Count = objs.Length;
            PreFillExistItems<T>(localPos);

            var newItemIndex = transform.childCount - 1;
            while (newItemIndex < Count)
            {
                var obj = Object.Instantiate(itemTemplate.gameObject, itemTemplate.parent, false);
                obj.name = newItemIndex.ToString();
                if (localPos != null && newItemIndex < localPos.Count)
                    obj.transform.localPosition = localPos[newItemIndex];

                _templateChildren.Add(obj);
                var view = new T
                {
                    gameObject = obj.transform.GetChild(0).gameObject
                };
                view.BindUI();
                view.onCreate();
                _children.Add(view);
                ++newItemIndex;
            }

            var containerTrans = itemTemplate.parent;
            var curItemIndex = 1;
            var curDataIndex = 0;
            while (curDataIndex < Count && curItemIndex < containerTrans.childCount && curDataIndex < _children.Count)
            {
                containerTrans.GetChild(curItemIndex).gameObject.SetActive(true);
                _children[curDataIndex].onShow(new object[] { objs[curDataIndex] });
                ++curItemIndex;
                ++curDataIndex;
            }

            while (curItemIndex < containerTrans.childCount)
            {
                containerTrans.GetChild(curItemIndex).gameObject.SetActive(false);
                ++curItemIndex;
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        /// <summary>
        /// 隐藏并销毁指定下标的子项。
        /// </summary>
        public void RemoveAt(int index)
        {
        if (index < 0 || index >= _children.Count || index >= _templateChildren.Count)
            return;

            _children[index].onHide();
            _children[index].OnDestroy();
            Object.DestroyImmediate(_templateChildren[index]);
        }

        /// <summary>
        /// 再复制一个子 View。param 目前只占位，不传入 onShow。
        /// </summary>
        public void Append<T>(object param) where T : UIViewBase, new()
        {
            var itemTemplate = FindTemplate();
            if (itemTemplate == null)
                return;

            var newItemIndex = _children.Count;
            var obj = Object.Instantiate(itemTemplate.gameObject, itemTemplate.parent, false);
            obj.name = newItemIndex.ToString();
            _templateChildren.Add(obj);
            var view = new T
            {
                gameObject = obj.transform.GetChild(0).gameObject
            };
            view.BindUI();
            view.onCreate();
            _children.Add(view);
        }

        /// <summary>
        /// 销毁已复制的子项，不动模板节点。
        /// </summary>
        public void Clear()
        {
            for (var i = 0; i < _children.Count; i++)
            {
                _children[i].onHide();
                _children[i].OnDestroy();
            }

            _children.Clear();
            for (var i = 0; i < _templateChildren.Count; i++)
                Object.DestroyImmediate(_templateChildren[i]);

            _templateChildren.Clear();
        }

        /// <summary>
        /// 按下标取子 View。越界返回 null。
        /// </summary>
        public UIViewBase GetViewByIndex(int index)
        {
            if (index >= _children.Count || index < 0)
            {
                G.LogError($"[UIContainer] GetViewByIndex index={index}, count={_children.Count}");
                return null;
            }

            return _children[index];
        }

        void PreFillExistItems<T>(List<Vector3> localPos = null) where T : UIViewBase, new()
        {
            for (var i = 1; i < transform.childCount; i++)
            {
                var curItemObj = transform.GetChild(i);
                curItemObj.name = (i - 1).ToString();
                var localPosIndex = i - 1;
                if (localPos != null && localPosIndex < localPos.Count)
                    curItemObj.transform.localPosition = localPos[localPosIndex];

                _templateChildren.Add(curItemObj.gameObject);
                var view = new T
                {
                    gameObject = curItemObj.transform.GetChild(0).gameObject
                };
                view.BindUI();
                view.onCreate();
                _children.Add(view);
            }
        }

        Transform FindTemplate()
        {
            if (transform.childCount < 1)
                return null;

            var itemTrans = transform.GetChild(0);
            if (itemTrans.name != Template)
            {
                G.LogError("[UIContainer] missing Template child");
                return null;
            }

            itemTrans.gameObject.SetActive(false);
            return itemTrans;
        }
    }
}
