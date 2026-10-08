using System;
using System.Collections;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

namespace Xease.UI
{
    /// <summary>
    /// 事件注册时机：Create 随 OnDestroy 清理，Show 随 onHide 清理。
    /// </summary>
    public enum EUIInitType
    {
        Create,
        Show,
    }

    /// <summary>
    /// 打开 UI 时注入的管理器与参数。
    /// </summary>
    public class UIParams
    {
        public static UIParams New(UIManager mng, object[] args)
        {
            return new UIParams { OwnerMng = mng, Args = args };
        }

        public UIManager OwnerMng;
        public object[] Args;
    }

    /// <summary>
    /// Panel/View 公共生命周期、子 View 字典与开合缩放。
    /// </summary>
    public abstract class UIBase : IScaleable
    {
        static readonly Vector3 ScaleHidden = new Vector3(0.01f, 0.01f, 1f); // 开合缩放起始/结束（近零，避免真正 0）
        static readonly Vector3 ScaleOvershoot = new Vector3(1.15f, 1.15f, 1f); // 开合缩放过冲
        static readonly Vector3 ScaleNormal = Vector3.one;
        const float ScaleUpDuration = 0.15f; // 放大到过冲的时长，秒
        const float ScaleSettleDuration = 0.05f; // 过冲落到 1 的时长，秒

        public bool isShow => _isShow;

        protected bool _isShow;

        [NonSerialized]
        public bool IsOpenScaleOver;

        public GameObject gameObject;
        public Transform root;
        public Transform scaleRoot; // 名为 Scale 的子节点，存在才播开合缩放
        public string Name;

        public Transform transform => gameObject.transform;

        public List<string> ViewNameList;

        List<RenderTexture> _rtList; // onHide 时 Release

        [NonSerialized]
        public DictionaryList<string, UIViewBase> UIViews; // 预制体节点 v_{name} 绑定的子 View

        public DictionaryList<string, UIViewBase> UIExtraArgsViews; // 运行时 AddView 挂上的额外 View

        public UIBase Parent;

        public string uiViewName;

        public int SortingOrder;

        readonly Dictionary<EUIInitType, List<ICloseClear>> _closeClearDict =
            new Dictionary<EUIInitType, List<ICloseClear>>();

        readonly List<MotionHandle> _motionList = new List<MotionHandle>(); // 关闭/销毁时 TryCancel

        readonly Dictionary<string, object[]> AddViewArgsDict = new Dictionary<string, object[]>();

        protected UIManager mOwnerMng;

        /// <summary>
        /// 注入所属 UIManager。
        /// </summary>
        public virtual void InitializeParams(UIParams uiParam)
        {
            mOwnerMng = uiParam.OwnerMng;
        }

        /// <summary>
        /// 资源尚未 Instantiate 前的异步准备，按需重写。
        /// </summary>
        public virtual IEnumerator Prepare()
        {
            yield break;
        }

        protected void AddViewName(List<string> viewNameList)
        {
            if (ViewNameList != null)
            {
                G.LogWarning("[UIBase] ViewNameList already set");
                return;
            }

            ViewNameList = viewNameList;
        }

        public virtual void Initialize()
        {
        }

        /// <summary>
        /// 按名显示已绑定的子 View。
        /// </summary>
        public void ShowView(string[] viewName)
        {
            for (int i = 0; i < viewName.Length; i++)
                ShowView(viewName[i]);
        }

        public void ShowView(string viewName)
        {
            if (UIViews != null && UIViews.TryGetValue(viewName, out UIViewBase viewBase))
            {
                viewBase.Show(null);
                return;
            }

            G.LogError($"[UIBase] ViewBase not found: {viewName}");
        }

        /// <summary>
        /// 按名隐藏已绑定的子 View（含 ExtraArgs）。
        /// </summary>
        public void HideView(string[] viewName)
        {
            for (int i = 0; i < viewName.Length; i++)
                HideView(viewName[i]);
        }

        public void HideView(string viewName)
        {
            if (UIViews != null && UIViews.TryGetValue(viewName, out UIViewBase viewBase))
                viewBase.Hide();
            else
                G.LogWarning($"[UIBase] ViewBase not found: {viewName}");

            if (UIExtraArgsViews == null)
                return;

            if (UIExtraArgsViews.TryGetValue(viewName, out UIViewBase extraViewBase))
                extraViewBase.Hide();
            else
                G.LogWarning($"[UIBase] Extra ViewBase not found: {viewName}");
        }

        public void ShowAllView()
        {
            if (UIViews == null)
                return;
            for (int i = 0; i < UIViews.Count; i++)
                UIViews.ValueList[i].Show(null);
        }

        public void HideAllView()
        {
            if (UIViews == null)
                return;
            for (int i = 0; i < UIViews.Count; i++)
                UIViews.ValueList[i].Hide();
        }

        public bool GetViewShowStatus(string viewName)
        {
            if (UIViews != null && UIViews.TryGetValue(viewName, out UIViewBase view))
                return view.isShow;
            return false;
        }

        /// <summary>
        /// 先查 UIViews 再查 ExtraArgs。
        /// </summary>
        public UIViewBase GetViewByName(string viewName)
        {
            if (UIViews != null && UIViews.TryGetValue(viewName, out UIViewBase view))
                return view;

            if (UIExtraArgsViews != null && UIExtraArgsViews.TryGetValue(viewName, out UIViewBase extraView))
                return extraView;

            return null;
        }

        /// <summary>
        /// 递归设置节点及其子节点 layer。
        /// </summary>
        public void SetChildLayer(Transform trans, int layer)
        {
            trans.gameObject.layer = layer;
            for (int i = 0; i < trans.childCount; i++)
                SetChildLayer(trans.GetChild(i), layer);
        }

        void ClearRT()
        {
            if (_rtList == null)
                return;
            for (int i = 0; i < _rtList.Count; i++)
                _rtList[i].Release();
            _rtList = null;
        }

        /// <summary>
        /// 绑定控件引用；生成代码或手写 Override。
        /// </summary>
        public virtual void BindUI()
        {
        }

        public virtual void RegisterEvents()
        {
        }

        public virtual void UnRegisterEvents()
        {
        }

        /// <summary>
        /// Instantiate 之后：注册事件、解析 Root/Scale、播打开缩放。
        /// </summary>
        public virtual void onCreate()
        {
            RegisterEvents();
            root = transform.Find("Root");
            scaleRoot = transform.Find("Scale");
            DoScale();
        }

        /// <summary>
        /// 显示时转发到子 View；ExtraArgs View 使用 AddView 时缓存的参数。
        /// </summary>
        public virtual void onShow(object[] objs)
        {
            _isShow = true;

            if (UIViews != null)
            {
                for (int i = 0; i < UIViews.Count; i++)
                    UIViews.ValueList[i].Show(objs);
            }

            if (UIExtraArgsViews == null)
                return;

            for (int i = 0; i < UIExtraArgsViews.Count; i++)
            {
                var key = UIExtraArgsViews.KeyList[i];
                if (AddViewArgsDict.TryGetValue(key, out var extraArgs))
                    UIExtraArgsViews.ValueList[i].Show(extraArgs);
                else
                    UIExtraArgsViews.ValueList[i].Show(null);
            }
        }

        public virtual void onUpdate()
        {
            if (UIViews != null)
            {
                for (int i = 0; i < UIViews.Count; i++)
                    UIViews.ValueList[i].onUpdate();
            }

            if (UIExtraArgsViews == null)
                return;

            for (int i = 0; i < UIExtraArgsViews.Count; i++)
                UIExtraArgsViews.ValueList[i].onUpdate();
        }

        public virtual void onHide()
        {
            _isShow = false;
            ClearRT();
            HandleHideClear();
        }

        public virtual void OnDestroy()
        {
            UnRegisterEvents();
            if (UIViews != null)
            {
                for (int i = 0; i < UIViews.Count; i++)
                    UIViews.ValueList[i].OnDestroy();
                UIViews.Clear();
                UIViews = null;
            }

            if (UIExtraArgsViews != null)
            {
                for (int i = 0; i < UIExtraArgsViews.Count; i++)
                    UIExtraArgsViews.ValueList[i].OnDestroy();
                UIExtraArgsViews.Clear();
                UIExtraArgsViews = null;
            }

            ClearMotionList();
            HandleDestroyClear();
            ClearCloseDict();
            AddViewArgsDict.Clear();
        }

        void ClearMotionList()
        {
            for (int i = 0; i < _motionList.Count; i++)
                _motionList[i].TryCancel();
            _motionList.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IScaleable:
        /// <summary>
        /// 打开缩放：过冲后落到 1；无 scaleRoot 则直接返回。
        /// </summary>
        public void DoScale()
        {
            if (scaleRoot == null)
                return;

            IsOpenScaleOver = false;
            scaleRoot.localScale = ScaleHidden;
            PlayScale(ScaleHidden, ScaleOvershoot, ScaleUpDuration, () =>
            {
                PlayScale(ScaleOvershoot, ScaleNormal, ScaleSettleDuration, () =>
                {
                    IsOpenScaleOver = true;
                });
            });
        }

        /// <summary>
        /// 关闭缩放：过冲后收到近零，完成后回调 GameObject 名。
        /// </summary>
        public void DoScaleReverse(Action<string> callBack)
        {
            if (scaleRoot == null)
                return;

            PlayScale(ScaleNormal, ScaleOvershoot, ScaleSettleDuration, () =>
            {
                PlayScale(ScaleOvershoot, ScaleHidden, ScaleUpDuration, () =>
                {
                    callBack?.Invoke(gameObject.name);
                });
            });
        }

        MotionHandle PlayScale(Vector3 from, Vector3 to, float duration, Action onComplete)
        {
            var handle = LMotion.Create(from, to, duration)
                .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
                .WithOnComplete(onComplete)
                .Bind(scaleRoot, static (v, t) => t.localScale = v);
            AddMotion(handle);
            return handle;
        }

        /// <summary>
        /// 必须在 onCreate 内调用；随后框架会自动 onShow。
        /// </summary>
        public T AddView<T>(string viewName, Transform root, object[] objs) where T : UIViewBase
        {
            return AddView(viewName, root, objs) as T;
        }

        /// <summary>
        /// 必须在 onCreate 内调用；随后框架会自动 onShow。
        /// </summary>
        public UIViewBase AddView(string viewName, Transform root, object[] objs)
        {
            AddViewArgsDict[viewName] = objs;
            return mOwnerMng.AddExtraArgsViewSync(viewName, root, this, objs);
        }

        public void AddMotion(MotionHandle handle)
        {
            _motionList.Add(handle);
        }

        /// <summary>
        /// 登记随 Hide（Show）或 OnDestroy（Create）清理的句柄。
        /// </summary>
        public void AddCloseClear(EUIInitType type, ICloseClear clear)
        {
            if (!_closeClearDict.TryGetValue(type, out var list))
            {
                list = new List<ICloseClear>();
                _closeClearDict.Add(type, list);
            }

            list.Add(clear);
        }

        public void HandleHideClear()
        {
            if (!_closeClearDict.TryGetValue(EUIInitType.Show, out var list) || list == null)
                return;

            for (int i = 0; i < list.Count; i++)
                list[i].ClearOnClose();
            list.Clear();
        }

        public void HandleDestroyClear()
        {
            if (!_closeClearDict.TryGetValue(EUIInitType.Create, out var list) || list == null)
                return;

            for (int i = 0; i < list.Count; i++)
                list[i].ClearOnClose();
            list.Clear();
        }

        public void ClearCloseDict()
        {
            _closeClearDict.Clear();
        }
    }
}
