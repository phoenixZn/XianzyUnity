using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
//using Script.Editor.Extended.Engine.ScanPreferences;
//using Script.Editor.Extended.Engine.ShaderScan;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public class VfxScanWindow : EditorWindow
    {
        const string PrefShowInspectorWarning = "VfxScan.ShowInspectorWarnings";
        const int ScanBatchSize = 6;

        public static VfxScanWindow Instance { get; private set; }

        public static bool ShowInspectorWarnings
        {
            get
            {
                if (Instance != null)
                    return Instance._showInspectorWarnings;
                return EditorPrefs.GetBool(PrefShowInspectorWarning, true);
            }
        }

        readonly List<VfxScanResult> _allResults = new List<VfxScanResult>();
        readonly List<VfxScanResult> _filtered = new List<VfxScanResult>();
        readonly List<string> _pendingPaths = new List<string>();

        string _scanPath;
        VfxScanBudgetConfig _config;
        VfxHealthStatus? _filterStatus;
        string _searchText = string.Empty;
        bool _scanning;
        bool _suspendWatch;
        bool _bindingType;
        int _scanIndex;
        bool _showInspectorWarnings = true;
        VfxScanDetailAsset _detailAsset;

        TextField _pathField;
        ObjectField _configField;
        ToolbarSearchField _searchField;
        Label _summaryLabel;
        Label _statusLabel;
        Button _filterAll;
        Button _filterError;
        Button _filterWarning;
        Button _filterPass;
        MultiColumnListView _listView;

        [MenuItem("Tools/特效资源扫描")]
        public static void ShowWindow()
        {
            var window = GetWindow<VfxScanWindow>("特效资源扫描");
            window.minSize = new Vector2(960, 480);
            window.Show();
        }

        void OnEnable()
        {
            Instance = this;
            VfxScanDirtyHub.AssetsChanged -= OnAssetsChanged;
            VfxScanDirtyHub.AssetsChanged += OnAssetsChanged;
            LoadProjectSettings();
            _showInspectorWarnings = EditorPrefs.GetBool(PrefShowInspectorWarning, true);

            if (_detailAsset == null)
            {
                _detailAsset = CreateInstance<VfxScanDetailAsset>();
                _detailAsset.hideFlags = HideFlags.DontSave | HideFlags.HideInHierarchy;
                _detailAsset.name = "特效扫描详情";
            }
        }

        void OnDisable()
        {
            VfxScanDirtyHub.AssetsChanged -= OnAssetsChanged;
            if (Instance == this)
                Instance = null;
            StopScanInternal(false);
            SaveProjectSettings();
        }

        void OnDestroy()
        {
            if (_detailAsset != null)
                DestroyImmediate(_detailAsset);
        }

        public void CreateGUI()
        {
            LoadProjectSettings();
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            BuildToolbar();
            BuildFilterBar();
            BuildList();
            BuildStatusBar();
            ApplyFilter();
        }

        void OnAssetsChanged(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (_scanning || _suspendWatch || _allResults.Count == 0)
                return;
            HandleAssetsChanged(importedAssets, deletedAssets, movedAssets, movedFromAssetPaths);
        }

        public void RescanPath(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath) || _scanning)
                return;

            int index = FindResultIndex(prefabPath);
            if (index < 0)
                return;

            VfxScanResult result = ScanOne(prefabPath);
            if (result == null)
                _allResults.RemoveAt(index);
            else
                _allResults[index] = result;
            ApplyFilter();
            RefreshSelectedDetail();
        }

        public static void PingResult(VfxScanResult result)
        {
            if (result == null || string.IsNullOrEmpty(result.PrefabPath))
                return;
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(result.PrefabPath);
            if (obj == null)
                return;
            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        public static void PingIssue(VfxScanResult result, VfxScanIssue issue)
        {
            if (issue != null && !string.IsNullOrEmpty(issue.assetPath))
            {
                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(issue.assetPath);
                if (asset != null)
                {
                    EditorGUIUtility.PingObject(asset);
                    Selection.activeObject = asset;
                    return;
                }
            }

            if (result == null || string.IsNullOrEmpty(result.PrefabPath))
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
            if (prefab == null)
                return;

            GameObject target = VfxHierarchyPath.FindGameObject(prefab, issue != null ? issue.hierarchyPath : string.Empty);
            UnityEngine.Object pingObj = target != null ? (UnityEngine.Object)target : prefab;
            EditorGUIUtility.PingObject(pingObj);
            Selection.activeObject = pingObj;
        }

        void BuildToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.style.flexDirection = FlexDirection.Row;
            toolbar.style.flexWrap = Wrap.Wrap;
            toolbar.style.paddingLeft = 6;
            toolbar.style.paddingRight = 6;
            toolbar.style.paddingTop = 4;
            toolbar.style.paddingBottom = 4;
            toolbar.style.alignItems = Align.Center;

            _pathField = new TextField("扫描路径") { value = _scanPath };
            _pathField.style.minWidth = 280;
            _pathField.style.flexGrow = 1;
            _pathField.RegisterValueChangedCallback(evt =>
            {
                _scanPath = evt.newValue;
                SaveProjectSettings();
            });
            _pathField.RegisterCallback<DragUpdatedEvent>(OnPathDragUpdated, TrickleDown.TrickleDown);
            _pathField.RegisterCallback<DragPerformEvent>(OnPathDragPerform, TrickleDown.TrickleDown);
            toolbar.Add(_pathField);

            var browse = new Button(PingScanPath) { text = "浏览", tooltip = "在 Project 中定位当前扫描路径" };
            browse.style.width = 52;
            toolbar.Add(browse);

            _configField = new ObjectField("预算配置")
            {
                objectType = typeof(VfxScanBudgetConfig),
                allowSceneObjects = false,
                value = _config
            };
            _configField.style.minWidth = 220;
            _configField.RegisterValueChangedCallback(evt =>
            {
                _config = evt.newValue as VfxScanBudgetConfig;
                SaveProjectSettings();
                ReevaluateAll();
            });
            toolbar.Add(_configField);

            var scanButton = new Button(StartScan) { text = "开始扫描" };
            scanButton.style.width = 88;
            toolbar.Add(scanButton);

            var exportButton = new Button(ExportCsv) { text = "导出报表" };
            exportButton.style.width = 88;
            toolbar.Add(exportButton);

            var shaderButton = new Button(OpenShaderScan) { text = "分析 Shader" };
            shaderButton.style.width = 92;
            shaderButton.tooltip = "将当前扫描结果中的材质交给 Shader 分析工具";
            toolbar.Add(shaderButton);

            var warnToggle = new Toggle("Inspector 显示 Warning") { value = _showInspectorWarnings };
            warnToggle.style.marginLeft = 8;
            warnToggle.RegisterValueChangedCallback(evt =>
            {
                _showInspectorWarnings = evt.newValue;
                EditorPrefs.SetBool(PrefShowInspectorWarning, _showInspectorWarnings);
                if (_detailAsset != null)
                    EditorUtility.SetDirty(_detailAsset);
            });
            toolbar.Add(warnToggle);

            rootVisualElement.Add(toolbar);
        }

        void BuildFilterBar()
        {
            var bar = new VisualElement();
            bar.style.flexDirection = FlexDirection.Row;
            bar.style.alignItems = Align.Center;
            bar.style.paddingLeft = 6;
            bar.style.paddingRight = 6;
            bar.style.paddingBottom = 4;

            _filterAll = CreateFilterButton("全部", null);
            _filterError = CreateFilterButton("违规", VfxHealthStatus.Error);
            _filterWarning = CreateFilterButton("警告", VfxHealthStatus.Warning);
            _filterPass = CreateFilterButton("达标", VfxHealthStatus.Pass);
            bar.Add(_filterAll);
            bar.Add(_filterError);
            bar.Add(_filterWarning);
            bar.Add(_filterPass);

            _searchField = new ToolbarSearchField();
            _searchField.style.minWidth = 180;
            _searchField.style.marginLeft = 8;
            _searchField.RegisterValueChangedCallback(evt =>
            {
                _searchText = evt.newValue ?? string.Empty;
                ApplyFilter();
            });
            bar.Add(_searchField);

            _summaryLabel = new Label();
            _summaryLabel.style.marginLeft = 12;
            _summaryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            bar.Add(_summaryLabel);

            rootVisualElement.Add(bar);
        }

        Button CreateFilterButton(string title, VfxHealthStatus? status)
        {
            var button = new Button(() =>
            {
                _filterStatus = status;
                ApplyFilter();
                RefreshFilterButtons();
            })
            {
                text = title
            };
            button.style.marginRight = 4;
            button.userData = status;
            return button;
        }

        void BuildList()
        {
            _listView = new MultiColumnListView
            {
                itemsSource = _filtered,
                selectionType = SelectionType.Single,
                virtualizationMethod = CollectionVirtualizationMethod.FixedHeight,
                fixedItemHeight = 28,
                showBorder = true,
                horizontalScrollingEnabled = true,
                sortingEnabled = true
            };
            _listView.style.flexGrow = 1;

            AddColumn("status", "状态", 56, false, true, MakeLabel, BindStatus);
            AddColumn("name", "特效名称", 220, true, true, MakeLabel, BindName);
            AddColumn("type", "分类", 120, false, true, MakeTypeDropdown, BindType);
            AddColumn("particleCount", "粒子系统数量", 110, false, true, MakeLabel, BindParticleCount);
            AddColumn("particles", "评估粒子数", 100, false, true, MakeLabel, BindParticles);
            AddColumn("materials", "材质数", 70, false, true, MakeLabel, BindMaterials);
            AddColumn("textureCount", "纹理数量", 80, false, true, MakeLabel, BindTextureCount);
            AddColumn("texture", "贴图最大尺寸", 110, false, true, MakeLabel, BindTexture);
            AddColumn("ping", "操作", 36, false, false, MakePingButton, BindPing);

            _listView.columnSortingChanged += OnColumnSortingChanged;
            _listView.selectionChanged += OnSelectionChanged;
            rootVisualElement.Add(_listView);
        }

        void AddColumn(
            string name,
            string title,
            float width,
            bool stretch,
            bool sortable,
            Func<VisualElement> makeCell,
            Action<VisualElement, int> bindCell)
        {
            _listView.columns.Add(new Column
            {
                name = name,
                title = title,
                width = width,
                minWidth = width,
                stretchable = stretch,
                sortable = sortable,
                resizable = true,
                makeCell = makeCell,
                bindCell = bindCell
            });
        }

        static VisualElement MakeLabel()
        {
            var label = new Label();
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.overflow = Overflow.Hidden;
            label.style.textOverflow = TextOverflow.Ellipsis;
            return label;
        }

        VisualElement MakeTypeDropdown()
        {
            var dropdown = new DropdownField();
            dropdown.label = string.Empty;
            dropdown.labelElement.style.display = DisplayStyle.None;
            dropdown.style.flexGrow = 1;
            dropdown.style.marginTop = 1;
            dropdown.style.marginBottom = 1;
            dropdown.style.marginLeft = 2;
            dropdown.style.marginRight = 2;
            dropdown.tooltip = "修改 Prefab 的分类 Label，选项来自预算配置。Label 优先于路径规则。";
            dropdown.RegisterValueChangedCallback(OnTypeChoiceChanged);
            return dropdown;
        }

        VisualElement MakePingButton()
        {
            var button = new Button();
            button.tooltip = "Ping";
            button.text = string.Empty;
            button.style.width = 22;
            button.style.height = 22;
            button.style.paddingLeft = 0;
            button.style.paddingRight = 0;
            button.style.paddingTop = 0;
            button.style.paddingBottom = 0;
            button.style.marginLeft = 4;
            button.style.marginRight = 4;
            button.style.alignItems = Align.Center;
            button.style.justifyContent = Justify.Center;

            Texture icon = EditorGUIUtility.IconContent("d_Search Icon").image;
            if (icon == null)
                icon = EditorGUIUtility.IconContent("Search Icon").image;
            if (icon != null)
            {
                var image = new Image { image = icon, scaleMode = ScaleMode.ScaleToFit };
                image.style.width = 14;
                image.style.height = 14;
                button.Add(image);
            }

            button.clicked += () =>
            {
                int index = button.userData is int i ? i : -1;
                if (index >= 0 && index < _filtered.Count)
                    PingResult(_filtered[index]);
            };
            return button;
        }

        void BindStatus(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            if (result == null)
            {
                label.text = string.Empty;
                return;
            }

            switch (result.status)
            {
                case VfxHealthStatus.Error:
                    label.text = "Error";
                    label.style.color = new Color(0.91f, 0.36f, 0.36f);
                    break;
                case VfxHealthStatus.Warning:
                    label.text = "Warn";
                    label.style.color = new Color(0.90f, 0.72f, 0.30f);
                    break;
                default:
                    label.text = "Pass";
                    label.style.color = new Color(0.36f, 0.72f, 0.36f);
                    break;
            }
        }

        void BindName(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            label.text = result != null ? result.PrefabName : string.Empty;
            label.tooltip = result != null ? result.PrefabPath : string.Empty;
        }

        void BindType(VisualElement element, int index)
        {
            var dropdown = (DropdownField)element;
            dropdown.userData = index;
            VfxScanResult result = GetFiltered(index);
            IReadOnlyList<VfxTypeBudget> budgets = GetBudgets();
            List<string> choices = VfxTypeResolver.GetClassificationChoices(budgets);

            _bindingType = true;
            try
            {
                dropdown.choices = choices;
                if (result == null)
                {
                    dropdown.SetValueWithoutNotify(choices.Count > 0 ? choices[0] : string.Empty);
                    dropdown.SetEnabled(false);
                    return;
                }

                dropdown.SetEnabled(!_scanning);
                string current = VfxTypeResolver.GetChoiceName(result.resolvedType, budgets);
                if (choices.Count > 0 && !choices.Contains(current))
                    current = choices[0];
                dropdown.SetValueWithoutNotify(current);
            }
            finally
            {
                _bindingType = false;
            }
        }

        void OnTypeChoiceChanged(ChangeEvent<string> evt)
        {
            if (_bindingType || _scanning)
                return;
            if (string.Equals(evt.newValue, evt.previousValue, StringComparison.Ordinal))
                return;

            var dropdown = evt.target as DropdownField;
            if (dropdown == null)
                return;

            int index = dropdown.userData is int i ? i : -1;
            VfxScanResult result = GetFiltered(index);
            if (result == null || string.IsNullOrEmpty(result.PrefabPath))
                return;

            IReadOnlyList<VfxTypeBudget> budgets = GetBudgets();
            VfxTypeBudget selected = VfxTypeResolver.FindBudgetByChoice(budgets, evt.newValue);
            if (selected == null)
                return;

            UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(result.PrefabPath);
            if (asset == null)
                return;

            _suspendWatch = true;
            try
            {
                VfxTypeResolver.SetClassification(asset, selected, budgets);
            }
            finally
            {
                _suspendWatch = false;
            }

            RescanPath(result.PrefabPath);
        }

        void BindParticles(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            if (result == null)
            {
                label.text = string.Empty;
                return;
            }

            int value = result.TotalDerivedParticles;
            label.text = value.ToString();
        }

        void BindParticleCount(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            label.text = result != null ? result.ParticleCount.ToString() : string.Empty;
        }

        void BindMaterials(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            label.text = result != null ? result.MaterialCount.ToString() : string.Empty;
        }

        void BindTexture(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            if (result == null || result.metrics == null || result.metrics.maxTextureWidth <= 0)
            {
                label.text = "无";
                return;
            }

            label.text = result.metrics.maxTextureWidth + "x" + result.metrics.maxTextureHeight;
        }

        void BindTextureCount(VisualElement element, int index)
        {
            var label = (Label)element;
            VfxScanResult result = GetFiltered(index);
            label.text = result != null ? result.TextureCount.ToString() : string.Empty;
        }

        void BindPing(VisualElement element, int index)
        {
            element.userData = index;
        }

        void BuildStatusBar()
        {
            _statusLabel = new Label("尚未扫描。选择目录后点击「开始扫描」。");
            _statusLabel.style.paddingLeft = 8;
            _statusLabel.style.paddingBottom = 6;
            _statusLabel.style.paddingTop = 4;
            rootVisualElement.Add(_statusLabel);
        }

        void OnSelectionChanged(IEnumerable<object> selected)
        {
            VfxScanResult result = null;
            foreach (object item in selected)
            {
                result = item as VfxScanResult;
                break;
            }

            ShowDetail(result);
        }

        void ShowDetail(VfxScanResult result)
        {
            if (_detailAsset == null)
                return;
            _detailAsset.result = result;
            _detailAsset.name = result != null ? result.PrefabName : "特效扫描详情";
            Selection.activeObject = _detailAsset;
            EditorUtility.SetDirty(_detailAsset);
        }

        void RefreshSelectedDetail()
        {
            if (_detailAsset == null || _detailAsset.result == null)
                return;

            int index = FindResultIndex(_detailAsset.result.PrefabPath);
            _detailAsset.result = index >= 0 ? _allResults[index] : null;
            EditorUtility.SetDirty(_detailAsset);
            Repaint();
        }

        void StartScan()
        {
            if (_scanning)
                return;

            if (string.IsNullOrEmpty(_scanPath) || !AssetDatabase.IsValidFolder(_scanPath))
            {
                EditorUtility.DisplayDialog("特效资源扫描", "请选择有效的 Assets 目录。", "确定");
                return;
            }

            _pendingPaths.Clear();
            _pendingPaths.AddRange(VfxPrefabScanner.FindPrefabPaths(_scanPath));
            _allResults.Clear();
            _scanIndex = 0;
            _scanning = true;
            VfxShaderRiskAnalyzer.ClearCache();
            ApplyFilter();
            _statusLabel.text = "正在扫描 " + _pendingPaths.Count + " 个 Prefab...";
            EditorApplication.update += TickScan;
        }

        void TickScan()
        {
            if (!_scanning)
                return;

            int total = _pendingPaths.Count;
            if (total == 0)
            {
                FinishScan();
                return;
            }

            for (int n = 0; n < ScanBatchSize && _scanIndex < total; n++)
            {
                string path = _pendingPaths[_scanIndex];
                float progress = (float)_scanIndex / total;
                if (EditorUtility.DisplayCancelableProgressBar("特效资源扫描", path, progress))
                {
                    StopScanInternal(true);
                    _statusLabel.text = "扫描已取消（已完成 " + _allResults.Count + " / " + total + "）。";
                    ApplyFilter();
                    return;
                }

                VfxScanResult result = ScanOne(path);
                if (result != null)
                    _allResults.Add(result);
                _scanIndex++;
            }

            ApplyFilter();
            if (_scanIndex >= total)
                FinishScan();
        }

        void FinishScan()
        {
            StopScanInternal(true);
            ApplyFilter();
            _statusLabel.text = "扫描完成，共 " + _allResults.Count + " 个特效。选中一行可在 Inspector 查看诊断详情。";
        }

        void StopScanInternal(bool clearProgressBar)
        {
            if (_scanning)
                EditorApplication.update -= TickScan;
            _scanning = false;
            if (clearProgressBar)
                EditorUtility.ClearProgressBar();
        }

        VfxScanResult ScanOne(string prefabPath)
        {
            VfxPrefabMetrics metrics = VfxPrefabScanner.Scan(prefabPath);
            if (metrics == null || metrics.particleSystems == null || metrics.particleSystems.Count == 0)
                return null;
            return VfxRuleEvaluator.Evaluate(metrics, GetBudgets(), GetPathRules());
        }

        void ReevaluateAll()
        {
            if (_allResults.Count == 0)
                return;

            IReadOnlyList<VfxTypeBudget> budgets = GetBudgets();
            for (int i = 0; i < _allResults.Count; i++)
                _allResults[i] = VfxRuleEvaluator.Reevaluate(_allResults[i], budgets, GetPathRules());
            ApplyFilter();
            RefreshSelectedDetail();
        }

        void ApplyFilter()
        {
            _filtered.Clear();
            int errorCount = 0;
            int warnCount = 0;
            int passCount = 0;

            for (int i = 0; i < _allResults.Count; i++)
            {
                VfxScanResult result = _allResults[i];
                if (result == null)
                    continue;

                switch (result.status)
                {
                    case VfxHealthStatus.Error: errorCount++; break;
                    case VfxHealthStatus.Warning: warnCount++; break;
                    default: passCount++; break;
                }

                if (_filterStatus.HasValue && result.status != _filterStatus.Value)
                    continue;
                if (!MatchesSearch(result))
                    continue;
                _filtered.Add(result);
            }

            if (_filterAll != null)
            {
                _filterAll.text = "全部 (" + _allResults.Count + ")";
                _filterError.text = "违规 (" + errorCount + ")";
                _filterWarning.text = "警告 (" + warnCount + ")";
                _filterPass.text = "达标 (" + passCount + ")";
            }

            int total = _allResults.Count;
            float rate = total > 0 ? passCount * 100f / total : 0f;
            if (_summaryLabel != null)
                _summaryLabel.text = total > 0 ? "达标率 " + rate.ToString("0.0") + "%" : string.Empty;

            RefreshFilterButtons();
            SortFiltered();
            if (_listView != null)
            {
                _listView.itemsSource = _filtered;
                _listView.RefreshItems();
            }
        }

        void OnColumnSortingChanged()
        {
            SortFiltered();
            if (_listView != null)
                _listView.RefreshItems();
        }

        void SortFiltered()
        {
            if (_listView == null || _filtered.Count <= 1)
                return;

            var sorts = new List<SortColumnDescription>();
            if (_listView.sortedColumns != null)
            {
                foreach (SortColumnDescription desc in _listView.sortedColumns)
                    sorts.Add(desc);
            }

            if (sorts.Count == 0)
                return;

            _filtered.Sort((a, b) =>
            {
                for (int i = 0; i < sorts.Count; i++)
                {
                    int cmp = CompareByColumn(a, b, sorts[i].columnName);
                    if (cmp == 0)
                        continue;
                    return sorts[i].direction == SortDirection.Descending ? -cmp : cmp;
                }

                return CompareStable(a, b);
            });
        }

        static int CompareStable(VfxScanResult a, VfxScanResult b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;
            return string.Compare(a.PrefabPath, b.PrefabPath, StringComparison.OrdinalIgnoreCase);
        }

        static int CompareByColumn(VfxScanResult a, VfxScanResult b, string columnName)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            switch (columnName)
            {
                case "status":
                    return ((int)a.status).CompareTo((int)b.status);
                case "name":
                    return string.Compare(a.PrefabName, b.PrefabName, StringComparison.OrdinalIgnoreCase);
                case "type":
                    return string.Compare(a.budgetDisplayName, b.budgetDisplayName, StringComparison.OrdinalIgnoreCase);
                case "particleCount":
                    return a.ParticleCount.CompareTo(b.ParticleCount);
                case "particles":
                    return a.TotalDerivedParticles.CompareTo(b.TotalDerivedParticles);
                case "materials":
                    return a.MaterialCount.CompareTo(b.MaterialCount);
                case "textureCount":
                    return a.TextureCount.CompareTo(b.TextureCount);
                case "texture":
                    int aMax = a.metrics != null ? Mathf.Max(a.metrics.maxTextureWidth, a.metrics.maxTextureHeight) : 0;
                    int bMax = b.metrics != null ? Mathf.Max(b.metrics.maxTextureWidth, b.metrics.maxTextureHeight) : 0;
                    return aMax.CompareTo(bMax);
                default:
                    return 0;
            }
        }

        bool MatchesSearch(VfxScanResult result)
        {
            if (string.IsNullOrEmpty(_searchText))
                return true;
            return (result.PrefabName != null && result.PrefabName.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0) ||
                   (result.PrefabPath != null && result.PrefabPath.IndexOf(_searchText, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        void RefreshFilterButtons()
        {
            SetFilterButtonStyle(_filterAll, !_filterStatus.HasValue);
            SetFilterButtonStyle(_filterError, _filterStatus == VfxHealthStatus.Error);
            SetFilterButtonStyle(_filterWarning, _filterStatus == VfxHealthStatus.Warning);
            SetFilterButtonStyle(_filterPass, _filterStatus == VfxHealthStatus.Pass);
        }

        static void SetFilterButtonStyle(Button button, bool selected)
        {
            if (button == null)
                return;
            button.style.unityFontStyleAndWeight = selected ? FontStyle.Bold : FontStyle.Normal;
            button.style.backgroundColor = selected ? new Color(0.24f, 0.37f, 0.55f) : StyleKeyword.Null;
        }

        void PingScanPath()
        {
            if (string.IsNullOrEmpty(_scanPath))
                return;

            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath<DefaultAsset>(_scanPath);
            if (obj == null)
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(_scanPath);
            if (obj == null)
                return;

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        void OnPathDragUpdated(DragUpdatedEvent evt)
        {
            DragAndDrop.visualMode = TryGetDraggedFolderPath(out _)
                ? DragAndDropVisualMode.Copy
                : DragAndDropVisualMode.Rejected;
            evt.StopPropagation();
        }

        void OnPathDragPerform(DragPerformEvent evt)
        {
            if (!TryGetDraggedFolderPath(out string folderPath))
                return;

            DragAndDrop.AcceptDrag();
            _scanPath = folderPath;
            if (_pathField != null)
                _pathField.SetValueWithoutNotify(_scanPath);
            SaveProjectSettings();
            evt.StopPropagation();
        }

        static bool TryGetDraggedFolderPath(out string folderPath)
        {
            folderPath = null;
            UnityEngine.Object[] refs = DragAndDrop.objectReferences;
            if (refs == null || refs.Length == 0)
            {
                string[] paths = DragAndDrop.paths;
                if (paths == null || paths.Length == 0)
                    return false;
                return TryConvertToAssetsFolder(paths[0], out folderPath);
            }

            string assetPath = AssetDatabase.GetAssetPath(refs[0]);
            return TryConvertToAssetsFolder(assetPath, out folderPath);
        }

        static bool TryConvertToAssetsFolder(string assetPath, out string folderPath)
        {
            folderPath = null;
            if (string.IsNullOrEmpty(assetPath))
                return false;

            assetPath = assetPath.Replace('\\', '/');
            if (!assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
                return false;

            if (AssetDatabase.IsValidFolder(assetPath))
            {
                folderPath = assetPath;
                return true;
            }

            string dir = System.IO.Path.GetDirectoryName(assetPath);
            if (string.IsNullOrEmpty(dir))
                return false;

            folderPath = dir.Replace('\\', '/');
            return AssetDatabase.IsValidFolder(folderPath);
        }

        void OpenShaderScan()
        {
            var paths = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < _allResults.Count; i++)
            {
                VfxScanResult result = _allResults[i];
                if (result == null || result.metrics == null || result.metrics.materials == null)
                    continue;
                for (int m = 0; m < result.metrics.materials.Count; m++)
                {
                    string path = result.metrics.materials[m].assetPath;
                    if (string.IsNullOrEmpty(path) || !seen.Add(path))
                        continue;
                    paths.Add(path);
                }
            }

            if (paths.Count == 0)
            {
                EditorUtility.DisplayDialog("特效资源扫描", "请先扫描，且结果中需要包含材质。", "确定");
                return;
            }

            //ShaderScanApi.OpenWithMaterialPaths(paths, "来自特效资源扫描");
        }

        void ExportCsv()
        {
            if (_allResults.Count == 0)
            {
                EditorUtility.DisplayDialog("特效资源扫描", "请先扫描再导出。", "确定");
                return;
            }

            string filePath = EditorUtility.SaveFilePanel("导出特效扫描报表", Application.dataPath, "VfxScanReport", "csv");
            if (string.IsNullOrEmpty(filePath))
                return;

            if (!VfxScanReportExporter.ExportCsv(_allResults, filePath, out string error))
            {
                EditorUtility.DisplayDialog("特效资源扫描", "导出失败：" + error, "确定");
                return;
            }

            ShowNotification(new GUIContent("报表已导出"));
        }

        void HandleAssetsChanged(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var dirtyPrefabs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            CollectDirtyPrefabs(importedAssets, dirtyPrefabs);
            CollectDirtyPrefabs(movedAssets, dirtyPrefabs);

            if (deletedAssets != null)
            {
                for (int i = 0; i < deletedAssets.Length; i++)
                    RemoveResult(deletedAssets[i]);
            }

            if (movedFromAssetPaths != null && movedAssets != null)
            {
                int count = Math.Min(movedFromAssetPaths.Length, movedAssets.Length);
                for (int i = 0; i < count; i++)
                    RenameResult(movedFromAssetPaths[i], movedAssets[i]);
            }

            foreach (string path in dirtyPrefabs)
            {
                int index = FindResultIndex(path);
                VfxScanResult result = ScanOne(path);
                if (index >= 0)
                {
                    if (result == null)
                        _allResults.RemoveAt(index);
                    else
                        _allResults[index] = result;
                }
            }

            if (dirtyPrefabs.Count > 0 || (deletedAssets != null && deletedAssets.Length > 0))
            {
                ApplyFilter();
                RefreshSelectedDetail();
            }
        }

        void CollectDirtyPrefabs(string[] paths, HashSet<string> dirtyPrefabs)
        {
            if (paths == null)
                return;

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];
                if (string.IsNullOrEmpty(path))
                    continue;

                if (path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    if (FindResultIndex(path) >= 0)
                        dirtyPrefabs.Add(path);
                    continue;
                }

                for (int r = 0; r < _allResults.Count; r++)
                {
                    VfxScanResult result = _allResults[r];
                    if (result == null || result.metrics == null || result.metrics.referencedAssetPaths == null)
                        continue;
                    if (result.metrics.referencedAssetPaths.Contains(path))
                        dirtyPrefabs.Add(result.PrefabPath);
                }
            }
        }

        void RemoveResult(string path)
        {
            int index = FindResultIndex(path);
            if (index >= 0)
                _allResults.RemoveAt(index);
        }

        void RenameResult(string fromPath, string toPath)
        {
            int index = FindResultIndex(fromPath);
            if (index < 0)
                return;
            if (!string.IsNullOrEmpty(toPath) && toPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
            {
                VfxScanResult result = ScanOne(toPath);
                if (result == null)
                    _allResults.RemoveAt(index);
                else
                    _allResults[index] = result;
            }
            else
                _allResults.RemoveAt(index);
        }

        int FindResultIndex(string prefabPath)
        {
            for (int i = 0; i < _allResults.Count; i++)
            {
                if (_allResults[i] != null &&
                    string.Equals(_allResults[i].PrefabPath, prefabPath, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        IReadOnlyList<VfxTypeBudget> GetBudgets()
        {
            if (_config != null && _config.budgets != null && _config.budgets.Count > 0)
                return _config.budgets;
            return VfxScanBuiltinBudgets.CreateDefaultList();
        }

        IReadOnlyList<VfxPathMatchRule> GetPathRules()
        {
            // KaboomScanPreferences prefs = KaboomScanPreferences.instance;
            // if (prefs != null && prefs.vfx != null && prefs.vfx.pathRules != null)
            //     return prefs.vfx.pathRules;
            return Array.Empty<VfxPathMatchRule>();
        }

        void LoadProjectSettings()
        {
            // KaboomScanPreferences prefs = KaboomScanPreferences.instance;
            // prefs.EnsureInitialized();
            // _scanPath = prefs.vfx.scanPath;
            // _config = prefs.vfx.budgetConfig;
            // if (_config == null)
            //     _config = KaboomScanPreferences.LoadOrCreateVfxBudget();
        }

        void SaveProjectSettings()
        {
            // if (_config == null)
            // {
            //     _config = KaboomScanPreferences.LoadOrCreateVfxBudget();
            //     if (_configField != null)
            //         _configField.SetValueWithoutNotify(_config);
            // }
            //
            // KaboomScanPreferences prefs = KaboomScanPreferences.instance;
            // if (prefs.vfx == null)
            //     prefs.vfx = new VfxScanProjectSettings();
            // prefs.vfx.scanPath = _scanPath ?? string.Empty;
            // prefs.vfx.budgetConfig = _config;
            // prefs.Save();
        }

        VfxScanResult GetFiltered(int index)
        {
            if (index < 0 || index >= _filtered.Count)
                return null;
            return _filtered[index];
        }
    }
}
