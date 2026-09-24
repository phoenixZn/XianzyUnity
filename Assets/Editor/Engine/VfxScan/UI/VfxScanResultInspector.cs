using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    [CustomEditor(typeof(VfxScanDetailAsset))]
    public class VfxScanResultInspector : UnityEditor.Editor
    {
        static GUIContent s_PingContent;

        public override void OnInspectorGUI()
        {
            var asset = (VfxScanDetailAsset)target;
            VfxScanResult result = asset != null ? asset.result : null;
            if (result == null || result.metrics == null)
            {
                EditorGUILayout.HelpBox("在扫描列表中选中一行后，这里会显示完整诊断与一键修复。", MessageType.Info);
                return;
            }

            DrawHeader(result);
            EditorGUILayout.Space(8);
            DrawMetrics(result);
            EditorGUILayout.Space(8);
            DrawIssues(result);
            EditorGUILayout.Space(8);
            DrawRawDetails(result);
        }

        static void DrawHeader(VfxScanResult result)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(result.PrefabName, EditorStyles.boldLabel);
            DrawPingButton(() => VfxScanWindow.PingResult(result));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField("路径", result.PrefabPath);
            EditorGUILayout.LabelField("评价类型", result.budgetDisplayName + "（" + result.resolvedType + "）");
            EditorGUILayout.LabelField("健康度", StatusLabel(result.status));
        }

        static void DrawMetrics(VfxScanResult result)
        {
            EditorGUILayout.LabelField("核心指标", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("评估粒子数", result.TotalDerivedParticles + " / 预算 " + result.budgetMaxParticles);
            EditorGUILayout.LabelField("粒子系统数量", result.ParticleCount.ToString());
            EditorGUILayout.LabelField("材质数", result.MaterialCount + " / 预算 " + result.budgetMaxMaterials);
            string tex = result.metrics.maxTextureWidth > 0
                ? result.metrics.maxTextureWidth + "x" + result.metrics.maxTextureHeight + " / 预算边长 " + result.budgetMaxTextureSize
                : "无";
            EditorGUILayout.LabelField("贴图最大尺寸", tex);
            EditorGUILayout.LabelField("纹理数量", result.TextureCount.ToString());
            EditorGUILayout.LabelField("节点 / 深度", result.metrics.hierarchy.nodeCount + " / " + result.metrics.hierarchy.maxDepth);
            EditorGUILayout.LabelField("风险模块", result.RiskModuleSummary);
        }

        static void DrawIssues(VfxScanResult result)
        {
            EditorGUILayout.LabelField("诊断详情", EditorStyles.boldLabel);
            if (result.issues == null || result.issues.Count == 0)
            {
                EditorGUILayout.HelpBox("所有指标均在预算范围内。", MessageType.Info);
                return;
            }

            bool showWarning = VfxScanWindow.ShowInspectorWarnings;
            var groups = new List<IssueGroup>();
            for (int i = 0; i < result.issues.Count; i++)
            {
                VfxScanIssue issue = result.issues[i];
                if (!showWarning && issue.severity == VfxHealthStatus.Warning)
                    continue;

                IssueGroup group = FindGroup(groups, issue);
                if (group == null)
                {
                    group = new IssueGroup { severity = issue.severity, title = issue.title, advice = issue.advice };
                    groups.Add(group);
                }

                group.issues.Add(issue);
            }

            if (groups.Count == 0)
            {
                EditorGUILayout.HelpBox("没有 Error。Warning 已在工具窗口中关闭显示。", MessageType.Info);
                return;
            }

            for (int g = 0; g < groups.Count; g++)
            {
                IssueGroup group = groups[g];
                Color old = GUI.backgroundColor;
                GUI.backgroundColor = group.severity == VfxHealthStatus.Error
                    ? new Color(0.85f, 0.40f, 0.40f)
                    : new Color(0.85f, 0.72f, 0.35f);

                EditorGUILayout.BeginVertical("box");
                GUI.backgroundColor = old;

                EditorGUILayout.LabelField("[" + StatusLabel(group.severity) + "] " + group.title, EditorStyles.boldLabel);
                EditorGUILayout.LabelField(group.advice, EditorStyles.wordWrappedLabel);
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("涉及对象（" + group.issues.Count + "）", EditorStyles.miniBoldLabel);

                for (int i = 0; i < group.issues.Count; i++)
                {
                    VfxScanIssue issue = group.issues[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(ObjectLabel(result, issue), EditorStyles.miniLabel);
                    DrawPingButton(() => VfxScanWindow.PingIssue(result, issue));
                    if (issue.quickFixKind != VfxQuickFixKind.None &&
                        GUILayout.Button(FixButtonLabel(issue), GUILayout.Width(140)))
                        ApplyFix(result, issue);
                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndVertical();
            }
        }

        static void DrawRawDetails(VfxScanResult result)
        {
            if (result.metrics.particleSystems == null)
                result.metrics.particleSystems = new List<VfxParticleSystemMetrics>();
            EditorGUILayout.LabelField("粒子系统（" + result.metrics.particleSystems.Count + "）", EditorStyles.boldLabel);
            for (int i = 0; i < result.metrics.particleSystems.Count; i++)
            {
                VfxParticleSystemMetrics ps = result.metrics.particleSystems[i];
                string name = string.IsNullOrEmpty(ps.hierarchyPath) ? ps.objectName : ps.hierarchyPath;
                EditorGUILayout.LabelField(
                    name,
                    "评估 " + ps.derivedParticles + " / Max " + ps.maxParticles +
                    "  面数 " + (ps.isMeshRenderMode ? ps.meshTriangleCount.ToString() : "Billboard"));
            }

            EditorGUILayout.Space(4);
            int matCount = result.metrics.materials != null ? result.metrics.materials.Count : 0;
            EditorGUILayout.LabelField("材质（" + matCount + "）", EditorStyles.boldLabel);
            if (result.metrics.materials != null)
            {
                for (int i = 0; i < result.metrics.materials.Count; i++)
                {
                    VfxMaterialMetrics mat = result.metrics.materials[i];
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(
                        string.IsNullOrEmpty(mat.materialName) ? mat.assetPath : mat.materialName,
                        string.IsNullOrEmpty(mat.shaderName) ? "无 Shader" : mat.shaderName);
                    DrawPingButton(() => PingAsset(mat.assetPath));
                    EditorGUILayout.EndHorizontal();
                }
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("贴图（" + result.metrics.textures.Count + "）", EditorStyles.boldLabel);
            for (int i = 0; i < result.metrics.textures.Count; i++)
            {
                VfxTextureMetrics tex = result.metrics.textures[i];
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(
                    tex.textureName,
                    tex.width + "x" + tex.height + (tex.isPot ? " POT" : " NPOT") +
                    (tex.mipmapEnabled ? " MipOn" : " MipOff") +
                    (tex.isUncompressed ? " 未压缩" : string.Empty));
                DrawPingButton(() => PingAsset(tex.assetPath));
                EditorGUILayout.EndHorizontal();
            }
        }

        static void DrawPingButton(System.Action onClick)
        {
            if (GUILayout.Button(GetPingContent(), GUILayout.Width(22), GUILayout.Height(18)))
            {
                if (onClick != null)
                    onClick();
            }
        }

        static GUIContent GetPingContent()
        {
            if (s_PingContent != null)
                return s_PingContent;

            GUIContent icon = EditorGUIUtility.IconContent("d_Search Icon");
            if (icon == null || icon.image == null)
                icon = EditorGUIUtility.IconContent("Search Icon");

            s_PingContent = new GUIContent(string.Empty, icon != null ? icon.image : null, "定位");
            return s_PingContent;
        }

        static IssueGroup FindGroup(List<IssueGroup> groups, VfxScanIssue issue)
        {
            string title = issue.title ?? string.Empty;
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i].severity == issue.severity && groups[i].title == title)
                    return groups[i];
            }

            return null;
        }

        static string ObjectLabel(VfxScanResult result, VfxScanIssue issue)
        {
            if (!string.IsNullOrEmpty(issue.hierarchyPath))
                return issue.hierarchyPath;
            if (!string.IsNullOrEmpty(issue.assetPath))
                return Path.GetFileName(issue.assetPath);
            return string.IsNullOrEmpty(result.PrefabName) ? "Prefab" : result.PrefabName;
        }

        static void PingAsset(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return;
            Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);
            if (asset == null)
                return;
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
        }

        static void ApplyFix(VfxScanResult result, VfxScanIssue issue)
        {
            if (!VfxQuickFix.TryApply(result, issue, out string message))
            {
                EditorUtility.DisplayDialog("一键修复", message, "确定");
                return;
            }

            VfxScanWindow window = VfxScanWindow.Instance;
            if (window != null)
            {
                window.ShowNotification(new GUIContent(message));
                window.RescanPath(result.PrefabPath);
            }
            else
            {
                Debug.Log(message);
            }
        }

        static string FixButtonLabel(VfxScanIssue issue)
        {
            switch (issue.quickFixKind)
            {
                case VfxQuickFixKind.DisableCollision: return "一键关闭 Collision";
                case VfxQuickFixKind.DisableTrigger: return "一键关闭 Trigger";
                case VfxQuickFixKind.FixMaxParticles: return "一键修正 MaxParticles";
                case VfxQuickFixKind.DisableMipmap: return "一键关闭 Mipmap";
                default: return "一键修复";
            }
        }

        static string StatusLabel(VfxHealthStatus status)
        {
            switch (status)
            {
                case VfxHealthStatus.Error: return "Error";
                case VfxHealthStatus.Warning: return "Warning";
                default: return "Pass";
            }
        }

        class IssueGroup
        {
            public VfxHealthStatus severity;
            public string title;
            public string advice;
            public readonly List<VfxScanIssue> issues = new List<VfxScanIssue>();
        }
    }
}
