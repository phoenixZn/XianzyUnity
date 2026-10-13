using EnhancedUI.EnhancedScroller;
using LitMotion;
using System;
using System.Collections.Generic;
using UnityEngine;
using static EnhancedUI.EnhancedScroller.EnhancedScroller;

namespace Xease.UI
{
    /// <summary>
    /// 循环列表对单项的选中、点击和尺寸回调。
    /// </summary>
    public interface ILoopScroll
    {
        int CurSelect { get; set; }

        void SetSelect(int index);
        void OnItemSelect(int index);
        void OnItemClick(int index);

        void SetSize(int index, Vector2 size);
        void SetSizeX(int index, int x);
        void SetSizeY(int index, int y);
        int GetDataCount();
    }

    /// <summary>
    /// 基于 ScrollerPro 的循环列表。项从引用池取出，跳转用 LitMotion 改 anchoredPosition。
    /// </summary>
    public class LoopScroll<Data, View> : ILoopScroll where Data : new() where View : LoopScrollItem, IReference, new()
    {
        ScrollerPro loopScroll;
        UIBase uiBase;

        public Dictionary<int, View> dict = new Dictionary<int, View>(); // 当前显示中的项，键为数据索引
        public List<Data> dataList = new List<Data>(); // 当前数据

        bool needSelectWithClick = true; // 点击是否同时改选中

        Action<object> clickFunc; // 点击时把该行数据传出
        Action<int, Data> selectAction; // 选中时传出索引和数据

        GameObject itemPrefab; // 子节点名为 item 的模板
        GameObject groupPrefab;
        Dictionary<int, Vector2> sizeDict = new Dictionary<int, Vector2>(); // 各项尺寸，缺省用模板尺寸
        public int CurSelect { get; set; } = -1; // 当前选中数据索引，未选为 -1

        public int StartIndex => loopScroll.StartDataIndex; // 可见区起始数据索引
        public int EndIndex => loopScroll.EndDataIndex; // 可见区结束数据索引
        public ScrollerPro ScrollerPro => loopScroll;

        /// <summary>
        /// 注册吸附完成回调，会覆盖之前的。
        /// </summary>
        public void AddOnScrollerSnapped(ScrollerSnappedDelegate callback)
        {
            loopScroll.scrollerSnapped = callback;
        }
        /// <summary>
        /// 清掉吸附完成回调。
        /// </summary>
        public void RemoveOnScrollerSnapped()
        {
            loopScroll.scrollerSnapped = null;
        }

        /// <summary>
        /// 注册滚动回调，会覆盖之前的。
        /// </summary>
        public void AddOnScrollerScrolled(ScrollerScrolledDelegate callback)
        {
            loopScroll.scrollerScrolled = callback;
        }

        /// <summary>
        /// 清掉滚动回调。
        /// </summary>
        public void RemoveOnScrollerScrolled()
        {
            loopScroll.scrollerScrolled = null;
        }

        private Vector2 _itemSize = Vector2.zero;
        /// <summary>
        /// 模板 item 的尺寸，首次读取后缓存。
        /// </summary>
        public Vector2 ItemSize
        {
            get
            {
                if (_itemSize == Vector2.zero)
                {
                    RectTransform rect = itemPrefab.transform as RectTransform;
                    _itemSize = rect.sizeDelta;
                }
                return _itemSize;
            }
        }

        /// <summary>
        /// 绑定列表组件。要求子节点名为 item。selectAction 非空时作为选中回调。
        /// </summary>
        public LoopScroll(ScrollerPro scrollerPro, UIBase uiBase, Action<int, Data> selectAction = null)
        {

            loopScroll = scrollerPro;
            this.uiBase = uiBase;
            itemPrefab = scrollerPro.transform.Find("item").gameObject;
            // loopScrollItemPool = new LoopScrollItemPool<View>(this, 100, itemPrefab, loopScroll.transform, uiBase);

            loopScroll.GetObjectHandler = GetObject;
            loopScroll.ReturnObjectHandler = ReturnObject;
            loopScroll.ProvideDataHandler = ProvideData;
            loopScroll.GetGroupSizeHandler = GetGroupSize;
            loopScroll.GetDataCountHandler = GetDataCount;


            InitGroup(itemPrefab);
            if (selectAction != null)
            {
                this.selectAction = selectAction;
            }
        }

        /// <summary>
        /// 按 item 模板创建组预制，并交给 ScrollerPro。
        /// </summary>
        public void InitGroup(GameObject itemPrefab)
        {
            itemPrefab.gameObject.SetActive(false);

            groupPrefab = new GameObject("groupPrefab");
            groupPrefab.SetActive(false);
            groupPrefab.transform.SetParent(loopScroll.transform);
            groupPrefab.AddComponent<RectTransform>();


            RectTransform groupRect = groupPrefab.transform as RectTransform;
            RectTransform itemRect = itemPrefab.transform as RectTransform;

            if (loopScroll.isGrid)
            {
                RectTransform loopScrollRect = loopScroll.transform as RectTransform;
                //GridLayoutGroup gridLayoutGroup = groupPrefab.AddComponent<GridLayoutGroup>();
                //gridLayoutGroup.spacing = new Vector2(loopScroll.Scroller.spacing, 0);
                //gridLayoutGroup.cellSize = itemRect.sizeDelta();
                if (loopScroll.scrollDirection == ScrollDirectionEnum.Vertical)
                {
                    groupRect.sizeDelta = new Vector2(loopScrollRect.sizeDelta.x, itemRect.sizeDelta.y);
                }
                else
                {
                    groupRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, loopScrollRect.sizeDelta.y);
                }
            }
            else
            {
                groupRect.sizeDelta = itemRect.sizeDelta;
            }

            GroupBase groupBase = groupPrefab.AddComponent<GroupBase>();

            groupBase.InitGroup(loopScroll, itemPrefab.transform);
            loopScroll.groupPrefab = groupBase;
        }

        //动态修改isGrid的时候需要调用这个
        /// <summary>
        /// isGrid 变化后重算组尺寸并重新初始化组。
        /// </summary>
        public void RefreshGroup()
        {
            RectTransform groupRect = groupPrefab.transform as RectTransform;
            RectTransform itemRect = itemPrefab.transform as RectTransform;

            if (loopScroll.isGrid)
            {
                RectTransform loopScrollRect = loopScroll.transform as RectTransform;

                if (loopScroll.scrollDirection == ScrollDirectionEnum.Vertical)
                {
                    groupRect.sizeDelta = new Vector2(loopScrollRect.sizeDelta.x, itemRect.sizeDelta.y);
                }
                else
                {
                    groupRect.sizeDelta = new Vector2(itemRect.sizeDelta.x, loopScrollRect.sizeDelta.y);
                }
            }
            else
            {
                groupRect.sizeDelta = itemRect.sizeDelta;
            }

            GroupBase groupBase = groupPrefab.GetComponent<GroupBase>();
            groupBase.InitGroup(loopScroll, itemPrefab.transform);
        }

        #region 回调注册
        /// <summary>
        /// 取出或复用一项，摆到组内。currentLength 是本组最后一项的数据索引。
        /// </summary>
        public void GetObject(GroupBase groupBase, int index, int currentLength)
        {
            if (!dict.ContainsKey(index))
            {
                View item = ReferencePool.Acquire<View>(); //loopScrollItemPool.GetOnPool();

                if (item.gameObject == null)
                {
                    GameObject obj = GameObject.Instantiate(itemPrefab, loopScroll.transform, false);
                    RectTransform objRect = obj.transform as RectTransform;
                    objRect.localPosition = Vector3.zero;
                    objRect.localRotation = Quaternion.identity;
                    objRect.localScale = Vector3.one;
                    item.Init(this, obj, uiBase);
                }
                item.gameObject.SetActive(true);


                item.index = index;
                item.gameObject.name = index.ToString();

                item.groupBase = groupBase;

                RectTransform rect = item.gameObject.transform as RectTransform;
                rect.anchorMin = new Vector2(0, 1);
                rect.anchorMax = new Vector2(0, 1);
                rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = sizeDict[index];
                rect.SetParent(groupBase.transform);


                rect.localRotation = Quaternion.identity;
                rect.localScale = Vector3.one;

                //当前组的第几个索引
                int currentIndex = index % loopScroll.NumberOfCellsPerRow;

                //当前组最后那个索引
                int maxIndex = currentLength;
                var endPos = Vector2.zero;

                //Log.Debug((index % loopScroll.NumberOfCellsPerRow).ToString());
                //int maxLength = groupBase.groupEnd;

                // if (loopScroll.isGrid)
                {
                    if (loopScroll.scrollDirection == ScrollDirectionEnum.Vertical)
                    {
                        //先只考虑item大小都一样的情况，直接用第0个当默认大小
                        endPos = new Vector2((sizeDict[groupBase.groupStart].x + loopScroll.Scroller.spacing) * (groupBase.gridCount - 1), 0);

                        //  var sizeX = (groupBase.groupSize.x - (endPos.x + sizeDict[maxIndex].x)); //左上对齐
                        var sizeX = (ScrollerPro.Container.sizeDelta.x - (endPos.x + sizeDict[maxIndex].x)); //左上对齐
                        var halfSizeX = sizeX / 2;

                        switch (loopScroll.pivotHorizontal)
                        {
                            case PivotHorizontal.Left:
                                rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex, 0);
                                break;
                            case PivotHorizontal.Center:
                                rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex + halfSizeX, 0);
                                break;
                            case PivotHorizontal.Middle:
                                endPos = Vector2.zero;
                                //因为每个sizeDict大小可能不一样所以遍历加一下
                                for (int i = groupBase.groupStart; i < maxIndex; i++)
                                {
                                    endPos.x += (sizeDict[i].x + loopScroll.Scroller.spacing);
                                }
                                sizeX = (ScrollerPro.Container.sizeDelta.x - (endPos.x + sizeDict[maxIndex].x)); //左上对齐
                                halfSizeX = sizeX / 2;
                                rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex + halfSizeX, 0);
                                break;
                            case PivotHorizontal.Right:
                                rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex + sizeX, 0);
                                break;
                        }
                    }
                    if (loopScroll.scrollDirection == ScrollDirectionEnum.Horizontal)
                    {
                        //先只考虑item大小都一样的情况，直接用第0个当默认大小
                        endPos = new Vector2(0, (sizeDict[groupBase.groupStart].y + loopScroll.Scroller.spacing) * (groupBase.gridCount - 1));

                        //var sizeY = (groupBase.groupSize.y - (endPos.y + sizeDict[maxIndex].y)); //左上对齐
                        var sizeY = (ScrollerPro.Container.sizeDelta.y - (endPos.y + sizeDict[maxIndex].y)); //左上对齐
                        var halfSizeY = sizeY / 2;

                        switch (loopScroll.pivotVertical)
                        {
                            case PivotVerticle.Top:
                                rect.anchoredPosition = new Vector2(0, -(sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex);
                                break;
                            case PivotVerticle.Center:
                                rect.anchoredPosition = new Vector2(0, -((sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex + halfSizeY));
                                break;
                            case PivotVerticle.Middle:
                                endPos = Vector2.zero;
                                //因为每个sizeDict大小可能不一样所以遍历加一下
                                for (int i = groupBase.groupStart; i < maxIndex; i++)
                                {
                                    endPos.y += (sizeDict[i].y + loopScroll.Scroller.spacing);
                                }
                                sizeY = (ScrollerPro.Container.sizeDelta.y - (endPos.y + sizeDict[maxIndex].y)); //左上对齐
                                halfSizeY = sizeY / 2;

                                rect.anchoredPosition = new Vector2(0, -((sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex + halfSizeY));
                                break;
                            case PivotVerticle.Bottom:
                                rect.anchoredPosition = new Vector2(0, -((sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex + sizeY));
                                break;
                        }
                    }

                }
                // else
                // {
                //     if (loopScroll.scrollDirection == ScrollDirectionEnum.Vertical)
                //     {
                //         switch (loopScroll.pivotHorizontal)
                //         {
                //             case PivotHorizontal.Left:
                //                 rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex, 0);
                //                 break;
                //             case PivotHorizontal.Center:
                //                 var halfSizeX = (ScrollerPro.Container.SizeDelta().x / 2) - (groupBase.groupSize.x / 2);//左上对齐
                //                 rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex + halfSizeX, 0);
                //                 break;
                //             case PivotHorizontal.Right:
                //                 var sizeX = (ScrollerPro.Container.SizeDelta().x) - (groupBase.groupSize.x);//左上对齐
                //                 rect.anchoredPosition = new Vector2((sizeDict[index].x + loopScroll.Scroller.spacing) * currentIndex + sizeX, 0);
                //                 break;
                //         }
                //     }
                //     if (loopScroll.scrollDirection == ScrollDirectionEnum.Horizontal)
                //     {
                //         switch (loopScroll.pivotVertical)
                //         {
                //             case PivotVerticle.Top:
                //                 rect.anchoredPosition = new Vector2(0, (sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex);
                //                 break;
                //             case PivotVerticle.Center:
                //                 var halfSizeY = -(ScrollerPro.Container.SizeDelta().y / 2) + (groupBase.groupSize.y / 2);//因为是左上对齐所以是-的
                //                 rect.anchoredPosition = new Vector2(0, (sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex + halfSizeY);
                //                 break;
                //             case PivotVerticle.Bottom:
                //                 var sizeY =  -(ScrollerPro.Container.SizeDelta().y) + (groupBase.groupSize.y);//因为是左上对齐所以是-的
                //                 rect.anchoredPosition = new Vector2(0,(sizeDict[index].y + loopScroll.Scroller.spacing) * currentIndex + sizeY);
                //                 break;
                //         }
                //     }
                //
                // }



                item.OnShow();
                dict.Add(index, item);


            }
        }
        private void ReturnObject(int index)
        {
            if (dict.ContainsKey(index))
            {
                dict[index].OnHide();
                ReferencePool.Release(dict[index]);
                //loopScrollItemPool.ReleaseOnPool(dict[index]);
                dict.Remove(index);
            }
        }
        private void ProvideData(int index)
        {
            if (dict.ContainsKey(index))
            {
                dict[index].UpdateData(dataList[index]);
            }
            else
            {
                G.LogError("[LoopScroll] ProvideData missing index " + index);
            }
        }

        //返回最大x或者y 如果是垂直列表就返回y， 垂直列表 grid排布的时候x可以随便调整
        private int GetGroupSize(int dataIndex)
        {
            int max = loopScroll.GetDataCountHandler();
            var groupStart = dataIndex * loopScroll.NumberOfCellsPerRow;

            Vector2 maxSize = Vector2.zero;
            for (int i = 0; i < loopScroll.NumberOfCellsPerRow; i++)
            {
                if (groupStart + i >= max) continue;
                var index = groupStart + i;
                if (sizeDict.ContainsKey(index))
                {
                    var temp = sizeDict[index];
                    if (temp.x > maxSize.x)
                    {
                        maxSize.x = temp.x;
                    }
                    if (temp.y > maxSize.y)
                    {
                        maxSize.y = temp.y;
                    }
                }
            }
            var groupSize = loopScroll.Scroller.scrollDirection == ScrollDirectionEnum.Vertical ? maxSize.y : maxSize.x;
            return (int)groupSize;
        }
        /// <summary>
        /// 数据条数。
        /// </summary>
        public int GetDataCount()
        {
            return dataList.Count;
        }
        #endregion


        /// <summary>
        /// 当前数据列表。
        /// </summary>
        public List<Data> GetDataList()
        {
            return dataList;
        }
        /// <summary>
        /// 当前显示中的项。
        /// </summary>
        public Dictionary<int, View> GetItemDict()
        {
            return dict;
        }

        /// <summary>
        /// 可见区第一项。没有则返回 null。
        /// </summary>
        public T GetStartView<T>() where T : LoopScrollItem
        {
            if (dict.TryGetValue(StartIndex, out var view))
            {
                return view as T;
            }
            return null;
        }
        /// <summary>
        /// 滚动位置对应的项。没有则返回 null。
        /// </summary>
        public T GetViewByScrollPosition<T>(float scrollPosition) where T : LoopScrollItem
        {
            var index = loopScroll.GetCellViewIndexAtPosition(scrollPosition);
            if (dict.TryGetValue(index, out var view))
            {
                return view as T;
            }
            return null;
        }

        /// <summary>
        /// 可见区最后一项。没有则返回 null。
        /// </summary>
        public T GetEndView<T>() where T : LoopScrollItem
        {
            if (dict.TryGetValue(EndIndex, out var view))
            {
                return view as T;
            }
            return null;
        }

        /// <summary>
        /// 设置点击回调，参数是该行数据。
        /// </summary>
        public void SetClickFunc(Action<object> func)
        {
            this.clickFunc = func;
        }

        /// <summary>
        /// 派发该索引的点击回调。
        /// </summary>
        public void OnItemClick(int index)
        {
            if (clickFunc != null)
            {
                clickFunc(dataList[index]);
            }
        }


        /// <summary>
        /// 设置选择某个item
        /// </summary>
        /// <param name="index"></param>
        public void SetSelect(int index)
        {
            if (dict.Count == 0 || dataList.Count == 0)
                return;
            CurSelect = index;
            if (CurSelect < dataList.Count)
            {
                OnItemSelect(CurSelect);
                OnItemClick(CurSelect);
            }
            else
            {
                OnItemSelect(0);
                OnItemClick(0);
            }
        }


        /// <summary>
        /// 选中变化时通知回调，并刷新已显示项的选中态。
        /// </summary>
        public void OnItemSelect(int index)
        {
            if (needSelectWithClick)
            {
                CurSelect = index;
                if (selectAction != null && index >= 0)
                {
                    selectAction(index, dataList[index]);
                }
                if (dict.Count > 0)
                {
                    foreach (var item in dict.Values)
                    {
                        item.OnItemSelect(index);
                    }
                }
            }
        }

        #region 设置大小
        /// <summary>
        /// 改某项尺寸并重载列表，尽量保持当前滚动位置。
        /// </summary>
        public void SetSize(int index, Vector2 size)
        {
            if (sizeDict.ContainsKey(index))
            {
                sizeDict[index] = size;
            }
            var groupIndex = index / loopScroll.NumberOfCellsPerRow;

            var cellPosition = loopScroll.Scroller.GetScrollPositionForCellViewIndex(groupIndex, CellViewPositionEnum.Before);
            var tweenCellOffset = cellPosition - loopScroll.Scroller.ScrollPosition;
            loopScroll.IgnoreLoopJump(true);
            loopScroll.Scroller.ReloadData();
            cellPosition = loopScroll.Scroller.GetScrollPositionForCellViewIndex(groupIndex, CellViewPositionEnum.Before);
            loopScroll.Scroller.SetScrollPositionImmediately(cellPosition - tweenCellOffset);
            loopScroll.IgnoreLoopJump(false);
        }
        /// <summary>
        /// 只改宽度。该项尚未有尺寸记录时不处理。
        /// </summary>
        public void SetSizeX(int index, int x)
        {
            if (sizeDict.ContainsKey(index))
            {
                var size = sizeDict[index];
                size.x = x;
                SetSize(index, size);
            }
        }
        /// <summary>
        /// 只改高度。该项尚未有尺寸记录时不处理。
        /// </summary>
        public void SetSizeY(int index, int y)
        {
            if (sizeDict.ContainsKey(index))
            {
                var size = sizeDict[index];
                size.y = y;
                SetSize(index, size);
            }
        }

        #endregion

        #region 预加载大小
        /// <summary>
        /// 预先写入某项尺寸，不立刻重排。
        /// </summary>
        public void PreloadSize(int index, Vector2 size)
        {
            if (sizeDict.ContainsKey(index))
            {
                sizeDict[index] = size;
            }
            else
            {
                sizeDict.Add(index, size);
            }
        }
        /// <summary>
        /// 预先写入宽度，高度用模板尺寸。
        /// </summary>
        public void PreloadSizeX(int index, int x)
        {
            if (sizeDict.ContainsKey(index))
            {
                sizeDict[index] = new Vector2(x, ItemSize.y);
            }
            else
            {
                sizeDict.Add(index, new Vector2(x, ItemSize.y));
            }
        }
        /// <summary>
        /// 预先写入高度，宽度用模板尺寸。
        /// </summary>
        public void PreloadSizeY(int index, int y)
        {
            if (sizeDict.ContainsKey(index))
            {
                sizeDict[index] = new Vector2(ItemSize.x, y);
            }
            else
            {
                sizeDict.Add(index, new Vector2(ItemSize.x, y));
            }
        }
        #endregion


        //public void RefershData()
        //{
        //    //dict.Clear();
        //    loopScroll.RefershData();
        //}

        /// <summary>
        /// 清理列表
        /// </summary>
        public void ClearList()
        {
            foreach (var item in dict.Values)
            {
                item.OnHide();
                ReferencePool.Release(item);
                // loopScrollItemPool.ReleaseOnPool(item);
            }

            ReferencePool.RemoveAll<View>();
            // loopScrollItemPool.ClearOnPool();
            loopScroll.ClearAll();
            dict.Clear();
            dataList.Clear();
            sizeDict.Clear();
        }


        /// <summary>
        /// 填充
        /// </summary>
        /// <param name="datas"></param>
        /// <param name="startItem"></param>
        /// <param name="loopJumpDirectionEnum"></param>
        public void Refill(List<Data> datas, int startItem = -1, LoopJumpDirectionEnum loopJumpDirectionEnum = LoopJumpDirectionEnum.Closest)
        {
            var oldCount = dataList.Count;

            dataList.Clear();

            if (datas == null || datas.Count == 0)
            {
                //清理一下上次的item
                if (oldCount > 0)
                {
                    foreach (var item in dict.Values)
                    {
                        ReferencePool.Release(item);
                        // loopScrollItemPool.ReleaseOnPool(item);
                    }
                    loopScroll.ClearAll();
                    dict.Clear();
                    loopScroll.ReloadData();
                }
                return;
            }

            dataList.AddRange(datas);

            for (int i = 0; i < datas.Count/*(datas.Count / loopScroll.NumberOfCellsPerRow) + 1*/; i++)
            {
                if (!sizeDict.ContainsKey(i))
                {
                    sizeDict.Add(i, ItemSize);
                }
            }


            if (oldCount == datas.Count)
            {
                var pos = loopScroll.ScrollPosition;
                loopScroll.RefreshActiveCellViews();


                loopScroll.ScrollPosition = pos;
            }
            else if (oldCount > 0)
            {
                foreach (var item in dict.Values)
                {
                    ReferencePool.Release(item);
                    // loopScrollItemPool.ReleaseOnPool(item);
                }
                loopScroll.ClearAll();
                dict.Clear();
                loopScroll.ReloadData();
            }
            else
            {
                loopScroll.ReloadData();
            }

            if (startItem >= 0)
            {
                JumpToDataIndex(startItem, loopJumpDirectionEnum: loopJumpDirectionEnum);
            }
            else
            {

            }
        }

        /// <summary>
        /// 追加数据并重载，保持当前滚动位置。
        /// </summary>
        public void Append(List<Data> datas)
        {
            dataList.AddRange(datas);

            for (int i = 0; i < dataList.Count/*(datas.Count / loopScroll.NumberOfCellsPerRow) + 1*/; i++)
            {
                if (!sizeDict.ContainsKey(i))
                {
                    sizeDict.Add(i, ItemSize);
                }
            }
            var pos = loopScroll.ScrollPosition;
            loopScroll.ReloadData();
            loopScroll.ScrollPosition = pos;
        }

        /// <summary>
        /// 跳转
        /// </summary>
        /// <param name="startItem"></param>
        /// <param name="jumpComplete"></param>
        /// <param name="tweenType"></param>
        /// <param name="tweenTime"></param>
        /// <param name="loopJumpDirectionEnum"></param>
        public void JumpToDataIndex(int startItem, Action jumpComplete = null, EnhancedScroller.TweenType tweenType = EnhancedScroller.TweenType.immediate, float tweenTime = 0, LoopJumpDirectionEnum loopJumpDirectionEnum = LoopJumpDirectionEnum.Closest, float scrollerOffset = 0, float cellOffset = 0)
        {
            if (startItem >= 0)
            {
                void Complete()
                {
                    jumpComplete?.Invoke();
                }
                loopScroll.JumpToDataIndex(startItem / loopScroll.NumberOfCellsPerRow, jumpComplete: Complete, tweenType: tweenType, tweenTime: tweenTime, loopJumpDirection: loopJumpDirectionEnum, scrollerOffset: scrollerOffset, cellOffset: cellOffset);
            }
        }

        /// <summary>
        /// 立即设置滚动位置。
        /// </summary>
        public void SetScrollPositionImmediately(float scrollPosition)
        {
            loopScroll.SetScrollPositionImmediately(scrollPosition);
        }

        /// <summary>
        /// 当前滚动位置。
        /// </summary>
        public float GetCurScrollPosition()
        {
            return loopScroll.ScrollPosition;
        }

        /// <summary>
        /// 把内容锚点动画到指定数据项。isForceSetCenter 为真时目标贴边，否则尽量把该项放在视口中间。
        /// </summary>
        public void JumpToIndex(int index, float duration = 0, Ease ease = Ease.Linear, Action jumpComplete = null, bool isForceSetCenter = false)
        {
            if (index < 0)
                return;

            switch (loopScroll.scrollDirection)
            {
                case ScrollDirectionEnum.Horizontal:
                    var containerWidth = loopScroll.Container.rect.width;
                    var itemWidth = itemPrefab.GetComponent<RectTransform>().rect.width;
                    var viewWidth = loopScroll.GetComponent<RectTransform>().rect.width;
                    var targetPosX = -index * (itemWidth + loopScroll.spacing) + (isForceSetCenter ? 0 : (viewWidth - itemWidth) / 2);
                    if (targetPosX >= 0)
                        targetPosX = 0;
                    else if (targetPosX <= -containerWidth)
                        targetPosX = -containerWidth + (viewWidth - itemWidth) / 2;

                    PlayAnchoredAxis(true, targetPosX, duration, ease, jumpComplete);
                    break;
                case ScrollDirectionEnum.Vertical:
                    var containerHeight = loopScroll.Container.rect.height;
                    var itemHeight = itemPrefab.GetComponent<RectTransform>().rect.height;
                    var viewHeight = loopScroll.GetComponent<RectTransform>().rect.height;
                    var targetPosY = index * (itemHeight + loopScroll.spacing) - (isForceSetCenter ? 0 : (viewHeight - itemHeight) / 2);
                    if (targetPosY <= 0)
                        targetPosY = 0;
                    else if (targetPosY >= containerHeight)
                        targetPosY = containerHeight - (viewHeight - itemHeight) / 2;

                    PlayAnchoredAxis(false, targetPosY, duration, ease, jumpComplete);
                    break;
            }
        }

        // 动画过程中同步滚动位置，忽略 timeScale
        void PlayAnchoredAxis(bool horizontal, float target, float duration, Ease ease, Action jumpComplete)
        {
            var container = loopScroll.Container;
            if (duration <= 0f)
            {
                var immediate = container.anchoredPosition;
                if (horizontal)
                    immediate.x = target;
                else
                    immediate.y = target;
                container.anchoredPosition = immediate;
                loopScroll.SetScrollPositionImmediately(target);
                jumpComplete?.Invoke();
                return;
            }

            var from = horizontal ? container.anchoredPosition.x : container.anchoredPosition.y;
            LMotion.Create(from, target, duration)
                .WithEase(ease)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(() =>
                {
                    loopScroll.SetScrollPositionImmediately(target);
                    jumpComplete?.Invoke();
                })
                .Bind(container, (value, rect) =>
                {
                    var pos = rect.anchoredPosition;
                    if (horizontal)
                        pos.x = value;
                    else
                        pos.y = value;
                    rect.anchoredPosition = pos;
                    loopScroll.SetScrollPositionImmediately(value);
                });
        }

        /// <summary>
        /// 设置可以不可以滑动
        /// </summary>
        /// <param name="enable"></param>
        public void SetScroll(bool enable)
        {
            loopScroll.SetScroll(enable);
        }


        /// <summary>
        /// 获取某个item
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        public View GetItem(int idx)
        {
            if (dict.TryGetValue(idx, out View view))
            {
                return view;
            }
            return null;
        }
    }
}