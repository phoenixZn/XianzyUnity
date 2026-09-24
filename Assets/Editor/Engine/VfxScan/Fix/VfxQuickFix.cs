using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxQuickFix
    {
        public static bool TryApply(VfxScanResult result, VfxScanIssue issue, out string message)
        {
            message = string.Empty;
            if (result == null || issue == null || issue.quickFixKind == VfxQuickFixKind.None)
            {
                message = "该问题不支持一键修复。";
                return false;
            }

            switch (issue.quickFixKind)
            {
                case VfxQuickFixKind.DisableCollision:
                    return SetParticleModuleEnabled(result.PrefabPath, issue.hierarchyPath, "CollisionModule.enabled", false, "关闭 Collision", out message);
                case VfxQuickFixKind.DisableTrigger:
                    return SetParticleModuleEnabled(result.PrefabPath, issue.hierarchyPath, "TriggerModule.enabled", false, "关闭 Trigger", out message);
                case VfxQuickFixKind.FixMaxParticles:
                    return SetMaxParticles(result.PrefabPath, issue.hierarchyPath, issue.quickFixIntValue, out message);
                case VfxQuickFixKind.DisableMipmap:
                    return DisableMipmap(issue.assetPath, out message);
                default:
                    message = "未知修复类型。";
                    return false;
            }
        }

        static bool SetParticleModuleEnabled(
            string prefabPath,
            string hierarchyPath,
            string propertyPath,
            bool enabled,
            string undoName,
            out string message)
        {
            if (!TryGetParticleSystem(prefabPath, hierarchyPath, out ParticleSystem ps, out string error))
            {
                message = error;
                return false;
            }

            SerializedObject so = new SerializedObject(ps);
            SerializedProperty prop = so.FindProperty(propertyPath);
            if (prop == null)
            {
                message = "找不到粒子模块属性：" + propertyPath;
                return false;
            }

            so.Update();
            Undo.RecordObject(ps, "特效扫描/" + undoName);
            prop.boolValue = enabled;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            message = undoName + " 已应用，可用 Ctrl+Z 撤回。";
            return true;
        }

        static bool SetMaxParticles(string prefabPath, string hierarchyPath, int value, out string message)
        {
            if (value <= 0)
            {
                message = "修正值无效。";
                return false;
            }

            if (!TryGetParticleSystem(prefabPath, hierarchyPath, out ParticleSystem ps, out string error))
            {
                message = error;
                return false;
            }

            SerializedObject so = new SerializedObject(ps);
            SerializedProperty prop = so.FindProperty("InitialModule.maxNumParticles");
            if (prop == null)
            {
                message = "找不到 Max Particles 属性。";
                return false;
            }

            so.Update();
            Undo.RecordObject(ps, "特效扫描/修正 Max Particles");
            prop.intValue = value;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ps);
            AssetDatabase.SaveAssets();
            message = "已将 Max Particles 修正为 " + value + "，可用 Ctrl+Z 撤回。";
            return true;
        }

        static bool DisableMipmap(string texturePath, out string message)
        {
            if (string.IsNullOrEmpty(texturePath))
            {
                message = "贴图路径为空。";
                return false;
            }

            var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null)
            {
                message = "找不到贴图导入设置：" + texturePath;
                return false;
            }

            Undo.RecordObject(importer, "特效扫描/关闭 Mipmap");
            importer.mipmapEnabled = false;
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
            message = "已关闭 Mipmap，可用 Ctrl+Z 撤回。";
            return true;
        }

        static bool TryGetParticleSystem(string prefabPath, string hierarchyPath, out ParticleSystem ps, out string error)
        {
            ps = null;
            error = string.Empty;
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                error = "无法加载 Prefab：" + prefabPath;
                return false;
            }

            GameObject go = VfxHierarchyPath.FindGameObject(prefab, hierarchyPath);
            if (go == null)
            {
                error = "找不到节点：" + hierarchyPath;
                return false;
            }

            ps = go.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                error = "该节点没有 ParticleSystem。";
                return false;
            }

            return true;
        }
    }
}
