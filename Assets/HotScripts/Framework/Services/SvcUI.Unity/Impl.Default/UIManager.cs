using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Xease.UI
{
    /// <summary>
    /// UI 服务实现：层节点、反射登记、Panel/Pop 导航栈、预制体加载。
    /// </summary>
    public class UIManager : IUIService, IEnvUpdate
    {
        const int OrderStep = 10; // 同层相邻 Panel 的 sortingOrder 间隔
        const int OrderHud = 0;
        const int OrderPanel = 10000;
        const int OrderPop = 20000;
        const int OrderTop = 25000;
        const int OrderOverTop = 26000;
        const int OrderFix = 30000;
        const float CreatedRootWorldY = 10000f; // 沿用场景根偏移，避免与 3D 相机重叠
        const float ReferenceResolutionX = 1080f; // CanvasScaler 设计分辨率宽，像素
        const float ReferenceResolutionY = 1920f; // CanvasScaler 设计分辨率高，像素
        const float ReferenceMatch = 0.5f; // 宽高同时适配时的权重，两端分别偏向宽或高
        const float UiCameraOrthoSize = 5f; // 正交相机半高，世界单位
        const float UiCameraNear = -10f; // 近裁剪面，世界单位
        const float UiCameraFar = 10f; // 远裁剪面，世界单位
        const float UiCameraDepth = 1f; // 画在主相机之后
        const float UiPlaneDistance = 0f; // Canvas 相对 UICamera 的平面距离
        const string CreatedRootName = "[UIRoot]";
        const string UiCameraName = "UICamera";
        const string CanvasRootName = "Root";
        const string UiLayerName = "UI";
        const string UrpCameraDataTypeName =
            "UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime";
        const string ViewNodePrefix = "v_";

        readonly List<Layer> _layers = new List<Layer>();
        readonly Dictionary<string, UILayerId> _name2layerDic = new Dictionary<string, UILayerId>();
        readonly StackList<string> uiStack = new StackList<string>(); // 仅 Panel、Pop 入栈
        readonly List<string> _opendUI = new List<string>();

        UIRoot _root;
        bool _ownsRoot; // true：Init 时创建的 DDOL 根，Shutdown 时销毁整个根

        public UIRoot root => _root;

        public Dictionary<string, ConstructorInfo> UIHandlerDic = new Dictionary<string, ConstructorInfo>();

        /// <summary>
        /// 解析或创建 UI 根、建层、扫描 [UIBaseHandler]。
        /// </summary>
        public void Init(GameObject rootObj = null)
        {
            if (rootObj == null)
            {
                G.LogError("UIService resolved == null");
                rootObj = CreateRuntimeRoot();
                _ownsRoot = true;
            }

            var rootTrans = rootObj.transform;
            rootTrans.localScale = Vector3.one;
            if (!_ownsRoot)
            {
                rootTrans.localPosition = new Vector3(0, CreatedRootWorldY, 0);
                rootTrans.localRotation = Quaternion.identity;
            }

            _root = new UIRoot();
            _root.gameObject = rootObj;
            _root.Init();

            if (_root.canvas == null)
            {
                G.LogError("[UIManager] UI root has no Canvas");
                return;
            }

            EnsureEventSystem();
            BuildLayers();
            CollectUI();
        }

        GameObject CreateRuntimeRoot()
        {
            var root = new GameObject(CreatedRootName);
            Object.DontDestroyOnLoad(root);

            var uiLayer = LayerMask.NameToLayer(UiLayerName);
            if (uiLayer < 0)
                G.LogError("[UIManager] layer UI is missing");

            var camGo = new GameObject(UiCameraName);
            camGo.transform.SetParent(root.transform, false);
            var cam = camGo.AddComponent<Camera>();
            // 不清颜色，只画 UI 层，叠在主相机之上
            cam.clearFlags = CameraClearFlags.Nothing;
            cam.orthographic = true;
            cam.orthographicSize = UiCameraOrthoSize;
            cam.nearClipPlane = UiCameraNear;
            cam.farClipPlane = UiCameraFar;
            cam.depth = UiCameraDepth;
            if (uiLayer >= 0)
                cam.cullingMask = 1 << uiLayer;
            EnsureUrpCameraData(camGo);

            var canvasGo = new GameObject(CanvasRootName, typeof(RectTransform));
            if (uiLayer >= 0)
                canvasGo.layer = uiLayer;
            canvasGo.transform.SetParent(root.transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = UiPlaneDistance;
            canvas.vertexColorAlwaysGammaSpace = true; // Linear 下顶点色留在 Gamma，由 UI Shader 转换以保持暗部精度
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ReferenceResolutionX, ReferenceResolutionY);
            scaler.matchWidthOrHeight = ReferenceMatch;
            canvasGo.AddComponent<GraphicRaycaster>();
            return root;
        }

        // URP 认相机靠 AdditionalCameraData；程序集未引用，按类型名挂上
        void EnsureUrpCameraData(GameObject camGo)
        {
            var urpType = Type.GetType(UrpCameraDataTypeName);
            if (urpType == null)
            {
                G.LogError("[UIManager] UniversalAdditionalCameraData type not found");
                return;
            }

            camGo.AddComponent(urpType);
        }

        void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
        }

        void BuildLayers()
        {
            _layers.Add(new Layer { id = UILayerId.Hud, order = OrderHud });
            _layers.Add(new Layer { id = UILayerId.Panel, order = OrderPanel });
            _layers.Add(new Layer { id = UILayerId.Pop, order = OrderPop });
            _layers.Add(new Layer { id = UILayerId.Top, order = OrderTop });
            _layers.Add(new Layer { id = UILayerId.OverTop, order = OrderOverTop });
            _layers.Add(new Layer { id = UILayerId.Fix, order = OrderFix });

            for (int i = 0, len = _layers.Count; i < len; i++)
            {
                var layer = _layers[i];
                var go = new GameObject("layer-" + layer.id);
                var trans = go.AddComponent<RectTransform>();
                trans.SetParent(_root.canvas.transform, false);
                trans.localScale = Vector3.one;
                trans.localPosition = Vector3.zero;
                trans.localRotation = Quaternion.identity;
                trans.anchoredPosition = Vector2.zero;
                trans.anchorMin = Vector2.zero;
                trans.anchorMax = Vector2.one;
                trans.sizeDelta = Vector2.zero;
                layer.root = go;
            }
        }

        void CollectUI()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var attrs = type.GetCustomAttributes(typeof(UIBaseHandlerAttribute), inherit: false);
                for (int j = 0; j < attrs.Length; j++)
                {
                    if (!typeof(UIBase).IsAssignableFrom(type))
                    {
                        G.LogError($"[UIManager] {type} is an invalid UILogicHandler");
                        continue;
                    }

                    var attr = (UIBaseHandlerAttribute)attrs[j];
                    ReflectionRegisterDic(attr, type.GetConstructor(Type.EmptyTypes));
                }
            }
        }

        void ReflectionRegisterDic(UIBaseHandlerAttribute attr, ConstructorInfo ci)
        {
            if (UIHandlerDic.TryGetValue(attr.PrefabName, out ConstructorInfo oldCi))
            {
                G.LogError($"[UIManager] Prefab:{attr.PrefabName} already has a handler {oldCi.DeclaringType}");
                return;
            }

            UIHandlerDic.Add(attr.PrefabName, ci);
        }

        GameObject LoadUiPrefab(string name)
        {
            var handle = G.Asset.LoadAssetSync<GameObject>(name, EAssetGroup.UI);
            var prefab = handle != null ? handle.AssetObject as GameObject : null;
            if (prefab == null)
                G.LogError($"[UIManager] invalid ui prefab: {name}");
            return prefab;
        }

        void FailOpen(string name)
        {
            _opendUI.Remove(name);
            if (uiStack.Contains(name))
                uiStack.RemoveData(name);
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        public void Shutdown()
        {
            HideAll();
            G.Asset.Release(EAssetGroup.UI);

            if (_ownsRoot && _root != null && _root.gameObject != null)
            {
                Object.Destroy(_root.gameObject);
            }
            else
            {
                for (int i = 0; i < _layers.Count; i++)
                {
                    if (_layers[i].root != null)
                        Object.Destroy(_layers[i].root);
                }
            }

            _layers.Clear();
            _name2layerDic.Clear();
            uiStack.Clear();
            _opendUI.Clear();
            UIHandlerDic.Clear();
            _root = null;
            _ownsRoot = false;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IUIService:
        /// <summary>
        /// UI 根上的相机；运行时根创建之后可用。
        /// </summary>
        public Camera UICamera => _root != null ? _root.camera : null;

        /// <summary>
        /// 异步打开；栈顶同名且已隐藏时只补 onShow。
        /// </summary>
        public void Show(string name, object[] args = null)
        {
            if (uiStack.Count > 0 && uiStack.Peek() == name)
            {
                var peekUI = Find(name);
                if (peekUI != null && !peekUI.isShow)
                    peekUI.onShow(args);
                return;
            }

            var cur = Find(name);
            if (cur != null)
            {
                HandleStackByReopenUI(name, cur, args);
                HideByNextOpen(name, cur);
                return;
            }

            if (!UIHandlerDic.TryGetValue(name, out ConstructorInfo ci))
            {
                G.LogError($"[UIManager] UI Name : {name} has no Attribute!");
                return;
            }

            cur = (UIPanelBase)ci?.Invoke(null);
            if (cur == null)
            {
                G.LogError("[UIManager] UIPanelBase reflection failed!");
                return;
            }

            cur.InitializeParams(UIParams.New(this, args));
            _opendUI.Add(name);
            HideByNextOpen(name, cur);
            Push2Stack(name, cur);
            G.Coroutines.StartCoroutine(PrepareShowUI(name, cur, args));
        }

        /// <summary>
        /// 关闭已打开面板。
        /// </summary>
        public void Hide(string name)
        {
            var cur = Find(name);
            if (cur == null)
                return;
            cur.Hide();
            CheckNeedOpenLastUI(cur);
        }

        /// <summary>
        /// 同步打开面板，慎用。
        /// </summary>
        public UIPanelBase ShowSync(string name, object[] args = null)
        {
            var cur = Find(name);
            if (cur != null)
            {
                if (uiStack.Count > 0 && uiStack.Peek() == name)
                {
                    if (!cur.isShow)
                        cur.onShow(args);
                    return cur;
                }

                HideByNextOpen(name, cur);
                HandleStackByReopenUI(name, cur, args);
                return cur;
            }

            if (!UIHandlerDic.TryGetValue(name, out ConstructorInfo ci))
            {
                G.LogError($"[UIManager] UI Name : {name} has no Attribute!");
                return null;
            }

            cur = (UIPanelBase)ci?.Invoke(null);
            if (cur == null)
            {
                G.LogError("[UIManager] UIPanelBase reflection failed!");
                return null;
            }

            cur.InitializeParams(UIParams.New(this, args));
            _opendUI.Add(name);
            HideByNextOpen(name, cur);
            if (!PrepareShowUISync(name, cur, args))
            {
                FailOpen(name);
                return null;
            }

            Push2Stack(name, cur);
            return cur;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEnvUpdate:
        /// <summary>
        /// 驱动已打开面板的 onUpdate（含开启动画完成检测）。
        /// </summary>
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            for (int i = _opendUI.Count - 1; i >= 0; i--)
            {
                var uiPanelBase = Find<UIPanelBase>(_opendUI[i]);
                if (uiPanelBase == null)
                    continue;
                uiPanelBase.onUpdate();
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>
        /// 栈顶 UI 名；栈空返回空字符串。
        /// </summary>
        public string GetShowFrontUI()
        {
            if (uiStack.Count > 0)
                return uiStack.Peek();
            return "";
        }

        /// <summary>
        /// 指定层的 RectTransform。
        /// </summary>
        public RectTransform GetLayerRoot(UILayerId uiLayerId)
        {
            for (int i = 0; i < _layers.Count; i++)
            {
                if (_layers[i].id == uiLayerId)
                    return _layers[i].root.GetComponent<RectTransform>();
            }

            return null;
        }

        Layer GetLayer(UILayerId id)
        {
            return _layers[(int)id];
        }

        /// <summary>
        /// 同步加载并挂到 parent 下的 View。
        /// </summary>
        public T AddView<T>(Transform parentTrans, string name) where T : UIViewBase
        {
            if (parentTrans == null)
            {
                G.LogError("[UIManager] add view without parent");
                return null;
            }

            var prefab = LoadUiPrefab(name);
            if (prefab == null)
                return null;

            var go = Object.Instantiate(prefab);
            if (!UIHandlerDic.TryGetValue(name, out ConstructorInfo ci))
                return null;

            T t = ci?.Invoke(null) as T;
            if (t == null)
            {
                G.LogError($"[UIManager] View = {name} is not in UIHandler");
                Object.Destroy(go);
                return null;
            }

            t.gameObject = go;
            var trans = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            trans.SetParent(parentTrans, false);
            trans.localScale = Vector3.one;
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.anchoredPosition = Vector2.zero;
            trans.anchorMin = Vector2.zero;
            trans.anchorMax = Vector2.one;
            trans.sizeDelta = Vector2.zero;
            trans.pivot = new Vector2(0.5f, 0.5f);
            t.BindUI();
            t.onCreate();
            t.onShow(null);
            return t;
        }

        /// <summary>
        /// 按层内已有面板分配 sortingOrder 并加入层列表。
        /// </summary>
        public void SetLayer(UIPanelBase ui)
        {
            var layer = GetLayer(ui.layerId);
            var curCount = layer.list.Count;
            var last = curCount == 0 ? null : layer.list[curCount - 1];
            ui.canvas.overrideSorting = true;
            ui.SortingOrder = last == null ? layer.order : last.SortingOrder + OrderStep;
            ui.SetSortingOrder();
            layer.list.Add(ui);
        }

        /// <summary>
        /// 把面板 RectTransform 挂到对应层根节点。
        /// </summary>
        public UIPanelBase Add(UIPanelBase ui)
        {
            var layer = GetLayer(ui.layerId);
            var trans = ui.gameObject.GetComponent<RectTransform>() ?? ui.gameObject.AddComponent<RectTransform>();
            trans.SetParent(layer.root.transform, false);
            trans.localScale = Vector3.one;
            trans.localPosition = Vector3.zero;
            trans.localRotation = Quaternion.identity;
            trans.anchoredPosition = Vector2.zero;
            trans.anchorMin = Vector2.zero;
            trans.anchorMax = Vector2.one;
            trans.sizeDelta = Vector2.zero;
            trans.pivot = new Vector2(0.5f, 0.5f);
            return ui;
        }

        /// <summary>
        /// 从层列表与打开列表移除，并尝试恢复栈内下层 Panel。
        /// </summary>
        public void Remove(UIPanelBase ui, bool hideAll)
        {
            var layer = GetLayer(ui.layerId);
            layer.list.Remove(ui);
            HidePopup(ui);
            CheckNeedOpenLastUI(ui);
            ui.gameObject.SetActive(false);
            _opendUI.Remove(ui.UIName);
        }

        /// <summary>
        /// 打开新 Panel 时隐藏栈内其它已显示 Panel；Pop 不触发。
        /// </summary>
        public void HideByNextOpen(string uiName, UIPanelBase open)
        {
            if (open.layerId != UILayerId.Panel)
                return;

            for (int i = 0; i < uiStack.DataList.Count; i++)
            {
                if (uiStack.DataList[i] == open.UIName)
                    continue;

                var stackUIName = uiStack.DataList[i];
                var stackUI = Find(stackUIName);
                if (stackUI == null)
                {
                    G.LogWarning($"[UIManager] HideByNextOpen({uiName}) stackUI == null, name={stackUIName}");
                    continue;
                }

                if (stackUI.isShow)
                    stackUI.HideByOpen();
            }
        }

        /// <summary>
        /// 按预制体名查找已实例化的面板。
        /// </summary>
        public UIPanelBase Find(string name)
        {
            if (!_name2layerDic.TryGetValue(name, out UILayerId id))
                return null;
            return _layers[(int)id].Find(name);
        }

        /// <summary>
        /// 是否仍在打开列表中（含被 HideByOpen 藏起的实例）。
        /// </summary>
        public bool IsPanelActive(string name)
        {
            return _opendUI.Contains(name);
        }

        /// <summary>
        /// 按预制体名查找指定类型的面板。
        /// </summary>
        public T Find<T>(string name) where T : UIPanelBase
        {
            return Find(name) as T;
        }

        /// <summary>
        /// 同步打开并转为 T。
        /// </summary>
        public T ShowSync<T>(string name) where T : UIPanelBase
        {
            return ShowSync(name) as T;
        }

        IEnumerator PrepareShowUI(string name, UIPanelBase uiPanelBase, object[] args)
        {
            yield return uiPanelBase.Prepare();

            var prefab = LoadUiPrefab(name);
            if (prefab == null)
            {
                FailOpen(name);
                yield break;
            }

            var go = Object.Instantiate(prefab);
            go.name = name;
            uiPanelBase.gameObject = go;
            uiPanelBase.uiViewName = name;
            _name2layerDic[name] = uiPanelBase.layerId;
            uiPanelBase.Initialize();
            uiPanelBase.BindUI();
            yield return ConstructViewBase(uiPanelBase);
            Add(uiPanelBase);
            SetLayer(uiPanelBase);
            uiPanelBase.onCreate();
            uiPanelBase.Show(name, args);
        }

        bool PrepareShowUISync(string name, UIPanelBase uiPanelBase, object[] args)
        {
            uiPanelBase.Prepare();
            var prefab = LoadUiPrefab(name);
            if (prefab == null)
                return false;

            var go = Object.Instantiate(prefab);
            go.name = name;
            uiPanelBase.gameObject = go;
            uiPanelBase.uiViewName = name;
            uiPanelBase.Initialize();
            _name2layerDic[name] = uiPanelBase.layerId;
            uiPanelBase.BindUI();
            ConstructViewBaseSync(uiPanelBase);
            Add(uiPanelBase);
            SetLayer(uiPanelBase);
            uiPanelBase.onCreate();
            uiPanelBase.Show(name, args);
            return true;
        }

        void ConstructViewBaseSync(UIBase uiBase)
        {
            if (uiBase.ViewNameList == null)
                return;

            for (int i = 0; i < uiBase.ViewNameList.Count; i++)
            {
                var viewName = uiBase.ViewNameList[i];
                if (!UIHandlerDic.TryGetValue(viewName, out ConstructorInfo ci))
                    continue;

                var viewBase = (UIViewBase)ci?.Invoke(null);
                if (viewBase == null)
                {
                    G.LogError($"[UIManager] UIBase -> {viewName}, has no attribute");
                    continue;
                }

                viewBase.Parent = uiBase;
                viewBase.uiViewName = viewName;
                viewBase.InitializeParams(UIParams.New(this, null));
                viewBase.Prepare();
                BindViewBase2Obj(uiBase.transform, viewName, viewBase);
                if (viewBase.gameObject == null)
                {
                    G.LogError($"[UIManager] {uiBase.gameObject.name} has no prefab node {viewName}");
                }
                else
                {
                    uiBase.UIViews ??= new DictionaryList<string, UIViewBase>();
                    uiBase.UIViews.Add(viewName, viewBase);
                    viewBase.BindUI();
                    viewBase.onCreate();
                }

                ConstructViewBaseSync(viewBase);
            }
        }

        IEnumerator ConstructViewBase(UIBase uiBase)
        {
            if (uiBase.ViewNameList == null)
                yield break;

            for (int i = 0; i < uiBase.ViewNameList.Count; i++)
            {
                var viewName = uiBase.ViewNameList[i];
                if (!UIHandlerDic.TryGetValue(viewName, out ConstructorInfo ci))
                    continue;

                var viewBase = (UIViewBase)ci?.Invoke(null);
                if (viewBase == null)
                    continue;

                viewBase.Parent = uiBase;
                viewBase.uiViewName = viewName;
                viewBase.InitializeParams(UIParams.New(this, null));
                yield return viewBase.Prepare();
                BindViewBase2Obj(uiBase.transform, viewName, viewBase);
                if (viewBase.gameObject == null)
                {
                    G.LogError($"[UIManager] {uiBase.gameObject.name} has no prefab node {viewName}");
                }
                else
                {
                    uiBase.UIViews ??= new DictionaryList<string, UIViewBase>();
                    uiBase.UIViews.Add(viewName, viewBase);
                    viewBase.BindUI();
                    viewBase.onCreate();
                }

                yield return ConstructViewBase(viewBase);
            }
        }

        /// <summary>
        /// 异步加载子 View 并挂到指定节点。
        /// </summary>
        public IEnumerator AddView(string name, Transform root, UIBase uiBase, object[] objs, Action onAddView = null)
        {
            if (!UIHandlerDic.TryGetValue(name, out ConstructorInfo ci))
                yield break;

            var viewBase = (UIViewBase)ci?.Invoke(null);
            viewBase.Parent = uiBase;
            viewBase.uiViewName = name;
            viewBase.InitializeParams(UIParams.New(this, objs));
            yield return viewBase.Prepare();

            var prefab = LoadUiPrefab(name);
            if (prefab == null)
                yield break;

            var go = Object.Instantiate(prefab);
            viewBase.gameObject = go;
            go.transform.SetParent(root);
            go.transform.localPosition = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            uiBase.UIViews ??= new DictionaryList<string, UIViewBase>();
            uiBase.UIViews.Add(name, viewBase);
            onAddView?.Invoke();
            viewBase.BindUI();
            viewBase.onCreate();
            viewBase.onShow(objs);
            yield return ConstructViewBase(viewBase);
        }

        /// <summary>
        /// 同步加载带额外参数的 View（供 UIBase.AddView 在 onCreate 中调用）。
        /// </summary>
        public UIViewBase AddExtraArgsViewSync(string name, Transform root, UIBase uiBase, object[] objs)
        {
            if (!UIHandlerDic.TryGetValue(name, out ConstructorInfo ci))
                return null;

            var viewBase = (UIViewBase)ci?.Invoke(null);
            viewBase.Parent = uiBase;
            viewBase.uiViewName = name;
            viewBase.InitializeParams(UIParams.New(this, objs));
            viewBase.Prepare();

            var prefab = LoadUiPrefab(name);
            if (prefab == null)
                return null;

            var go = Object.Instantiate(prefab);
            viewBase.gameObject = go;
            go.transform.SetParent(root);
            go.transform.localPosition = Vector3.zero;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            uiBase.UIExtraArgsViews ??= new DictionaryList<string, UIViewBase>();
            uiBase.UIExtraArgsViews.Add(name, viewBase);
            viewBase.BindUI();
            viewBase.onCreate();
            ConstructViewBaseSync(viewBase);
            return viewBase;
        }

        void BindViewBase2Obj(Transform parent, string viewName, UIViewBase viewBase)
        {
            string fixedViewName = ViewNodePrefix + viewName;
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == fixedViewName)
                {
                    viewBase.gameObject = child.gameObject;
                    return;
                }

                BindViewBase2Obj(child, viewName, viewBase);
                if (viewBase.gameObject != null)
                    return;
            }
        }

        void Push2Stack(string uiName, UIPanelBase ui)
        {
            if (ui.layerId != UILayerId.Panel && ui.layerId != UILayerId.Pop)
                return;
            uiStack.Push(uiName);
        }

        void HandleStackByReopenUI(string uiName, UIPanelBase uiPanel, object[] args)
        {
            var stackList = uiStack.DataList;
            bool find = false;
            for (int i = 0; i < stackList.Count; i++)
            {
                if (stackList[i] == uiPanel.gameObject.name)
                {
                    find = true;
                    break;
                }
            }

            if (find)
            {
                HandleStackInternal(uiPanel.layerId, uiName, new StackList<string>(), 0, args);
                return;
            }

            if (!uiPanel.isShow)
                uiPanel.Show(uiName, args);
        }

        void HandleStackInternal(UILayerId layerId, string uiName, StackList<string> stack, int sortingOrder,
            object[] args)
        {
            if (uiStack.Count > 0 && uiStack.Peek() == uiName)
            {
                var curPanel = uiStack.Pop();
                while (stack.Count > 0)
                    uiStack.Push(stack.Pop());

                var uiPanel = Find(uiName);
                uiPanel.SpecifySortingOrder(sortingOrder);
                if (!uiPanel.isShow)
                    uiPanel.Show(uiName, args);
                uiStack.Push(curPanel);
                return;
            }

            if (uiStack.Count <= 0)
            {
                while (stack.Count > 0)
                    uiStack.Push(stack.Pop());
                G.LogError("[UIManager] reopen UI not found in stack");
                return;
            }

            var pop = uiStack.Pop();
            var popPanel = Find(pop);
            if (popPanel != null)
            {
                if (popPanel.layerId == layerId)
                {
                    if (popPanel.SortingOrder > sortingOrder)
                        sortingOrder = popPanel.SortingOrder;
                    popPanel.SpecifySortingOrder(popPanel.SortingOrder - OrderStep);
                }
            }
            else
            {
                G.LogWarning($"[UIManager] stack entry lost instance: {pop}");
            }

            stack.Push(pop);
            HandleStackInternal(layerId, uiName, stack, sortingOrder, args);
        }

        void HidePopup(UIPanelBase ui)
        {
            if (uiStack.Count <= 0)
                return;
            if (ui.layerId != UILayerId.Panel && ui.layerId != UILayerId.Pop)
                return;

            if (ui.UIName == uiStack.Peek())
            {
                uiStack.Pop();
                return;
            }

            if (uiStack.DataList.Contains(ui.UIName))
                PopupPanelNotPeek(new StackList<string>(), ui);
        }

        void PopupPanelNotPeek(StackList<string> stack, UIPanelBase ui)
        {
            if (uiStack.Count <= 0)
                return;

            if (uiStack.Peek() == ui.UIName)
            {
                uiStack.Pop();
                while (stack.Count > 0)
                    uiStack.Push(stack.Pop());
                return;
            }

            if (uiStack.Count <= 0)
            {
                while (stack.Count > 0)
                    uiStack.Push(stack.Pop());
                G.LogError("[UIManager] hide UI not found in stack");
                return;
            }

            stack.Push(uiStack.Pop());
            PopupPanelNotPeek(stack, ui);
        }

        /// <summary>
        /// 栈内是否存在被隐藏、关闭后需要恢复的 Panel。
        /// </summary>
        public bool HaveNeedOpenLastUI(UIPanelBase curHideBase)
        {
            if (uiStack.Count <= 0)
                return false;

            for (int i = uiStack.DataList.Count - 1; i >= 0; i--)
            {
                var ui = Find(uiStack.DataList[i]);
                if (ui == null)
                    continue;
                if (ui.layerId == UILayerId.Panel && !ui.isShow)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// 关闭 Panel/Pop 后恢复栈内下层 Panel，途中的 Pop 一并显示。
        /// </summary>
        public void CheckNeedOpenLastUI(UIPanelBase curHideBase)
        {
            if (uiStack.Count <= 0)
                return;
            if (curHideBase.layerId != UILayerId.Panel && curHideBase.layerId != UILayerId.Pop)
                return;

            for (int i = uiStack.DataList.Count - 1; i >= 0; i--)
            {
                var uiName = uiStack.DataList[i];
                var ui = Find(uiName);
                if (ui == null)
                {
                    G.LogWarning($"[UIManager] CheckNeedOpenLastUI ui == null, uiName={uiName}");
                    continue;
                }

                if (ui.layerId == UILayerId.Panel && curHideBase.layerId != UILayerId.Pop)
                {
                    if (!ui.isShow)
                        ui.Show(uiName);
                    break;
                }

                if (ui.layerId == UILayerId.Pop && !ui.isShow)
                    ui.Show(uiName);
            }
        }

        /// <summary>
        /// 关闭除 ignore 外的各层面板（不含 Fix 层）。
        /// </summary>
        public void HideAll(List<string> ignoreUIPanel = null)
        {
            ClearUIStack(ignoreUIPanel);
            for (int i = 0; i < _layers.Count - 1; i++)
            {
                var layer = GetLayer(_layers[i].id);
                for (int j = layer.list.Count - 1; j >= 0; j--)
                {
                    var ui = layer.list[j];
                    if (ignoreUIPanel != null && ignoreUIPanel.Contains(ui.UIName))
                        continue;
                    ui.Hide(true);
                }
            }
        }

        /// <summary>
        /// 关闭指定层全部面板。
        /// </summary>
        public void HideAllLayer(UILayerId uiLayerId)
        {
            for (int i = 0; i < _layers.Count - 1; i++)
            {
                var layer = GetLayer(_layers[i].id);
                if (layer.id != uiLayerId)
                    continue;
                for (int j = layer.list.Count - 1; j >= 0; j--)
                    layer.list[j].Hide(true);
                break;
            }
        }

        /// <summary>
        /// 预留：按 ignore 重建栈；当前关闭路径已自行弹栈。
        /// </summary>
        public void ClearUIStack(List<string> ignoreUIPanel = null)
        {
        }

        /// <summary>
        /// 清空导航栈，不销毁已打开实例。
        /// </summary>
        public void Clear()
        {
            uiStack.Clear();
        }

        /// <summary>
        /// 指定面板开启动画是否结束；找不到面板视为已结束。
        /// </summary>
        public bool IsPanelScaleOver(string name)
        {
            UIPanelBase panelBase = Find(name);
            if (panelBase == null)
                return true;
            return panelBase.CheckPanelScaleOver() || !panelBase.CheckPanelHasNeedDoScale();
        }

        /// <summary>
        /// 是否为栈顶且当前没有 Pop 盖住。
        /// </summary>
        public bool CheckIsOnTheTop(string panelName)
        {
            if (_layers[(int)UILayerId.Pop].list.Count > 0)
            {
                if (_layers[(int)UILayerId.Pop].list.Last().UIName == panelName)
                    return true;
            }

            return uiStack.Count != 0 && uiStack.Peek() == panelName && !CheckIsPopping();
        }

        /// <summary>
        /// Pop 层是否有仍挂着的面板。
        /// </summary>
        public bool CheckIsPopping()
        {
            return _layers[(int)UILayerId.Pop].list.Count > 0;
        }

        class Layer
        {
            public UILayerId id;
            public int order; // 该层 sortingOrder 基值
            public GameObject root;
            public List<UIPanelBase> list = new List<UIPanelBase>();

            public UIPanelBase Find(string name)
            {
                return list.Find(ui => ui.gameObject != null && name == ui.gameObject.name);
            }
        }
    }
}
