using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxPrefabScanner
    {
        public static VfxPrefabMetrics Scan(string prefabPath)
        {
            var metrics = new VfxPrefabMetrics
            {
                prefabPath = prefabPath,
                prefabName = System.IO.Path.GetFileNameWithoutExtension(prefabPath)
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                return metrics;

            metrics.assetGuid = AssetDatabase.AssetPathToGUID(prefabPath);
            metrics.labels = VfxTypeResolver.GetLabels(prefab);
            metrics.referencedAssetPaths.Add(prefabPath);

            Transform root = prefab.transform;
            ParticleSystem[] systems = prefab.GetComponentsInChildren<ParticleSystem>(true);
            int totalDerived = 0;
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (!VfxHierarchyPath.IsOwnedByPrefab(ps.gameObject, prefab))
                    continue;

                VfxParticleSystemMetrics psMetrics = VfxParticleCollector.Collect(ps, root);
                metrics.particleSystems.Add(psMetrics);
                totalDerived += psMetrics.derivedParticles;
            }

            metrics.totalDerivedParticles = totalDerived;
            if (metrics.particleSystems.Count == 0)
                return metrics;

            VfxMaterialCollector.Collect(prefab, metrics.materials, metrics.referencedAssetPaths);
            VfxMaterialCollector.FillParticleMaterialPaths(prefab, metrics.particleSystems);
            VfxTextureCollector.Collect(prefab, metrics.materials, metrics.textures, metrics.referencedAssetPaths);
            metrics.hierarchy = VfxHierarchyCollector.Collect(prefab);

            int maxW = 0;
            int maxH = 0;
            for (int i = 0; i < metrics.textures.Count; i++)
            {
                VfxTextureMetrics tex = metrics.textures[i];
                if (tex.width > maxW)
                    maxW = tex.width;
                if (tex.height > maxH)
                    maxH = tex.height;
            }

            metrics.maxTextureWidth = maxW;
            metrics.maxTextureHeight = maxH;
            return metrics;
        }

        public static List<string> FindPrefabPaths(string scanRoot)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(scanRoot) || !AssetDatabase.IsValidFolder(scanRoot))
                return result;

            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { scanRoot });
            for (int i = 0; i < (guids != null ? guids.Length : 0); i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (!string.IsNullOrEmpty(path) && path.EndsWith(".prefab", System.StringComparison.OrdinalIgnoreCase))
                    result.Add(path);
            }

            return result;
        }
    }
}
