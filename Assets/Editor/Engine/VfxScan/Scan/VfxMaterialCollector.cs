using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxMaterialCollector
    {
        public static void Collect(
            GameObject prefabRoot,
            List<VfxMaterialMetrics> materials,
            List<string> referencedAssetPaths)
        {
            if (prefabRoot == null)
                return;

            var seen = new HashSet<int>();
            Renderer[] renderers = prefabRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !VfxHierarchyPath.IsOwnedByPrefab(renderer.gameObject, prefabRoot))
                    continue;
                CollectFromRenderer(renderer, seen, materials, referencedAssetPaths);
            }
        }

        public static void FillParticleMaterialPaths(GameObject prefabRoot, List<VfxParticleSystemMetrics> particleSystems)
        {
            if (prefabRoot == null || particleSystems == null)
                return;

            for (int i = 0; i < particleSystems.Count; i++)
            {
                VfxParticleSystemMetrics psMetrics = particleSystems[i];
                GameObject go = VfxHierarchyPath.FindGameObject(prefabRoot, psMetrics.hierarchyPath);
                if (go == null)
                    continue;

                ParticleSystemRenderer renderer = go.GetComponent<ParticleSystemRenderer>();
                if (renderer == null)
                    continue;

                Material mat = renderer.sharedMaterial;
                if (mat != null)
                {
                    psMetrics.materialAssetPath = AssetDatabase.GetAssetPath(mat);
                    psMetrics.renderQueue = mat.renderQueue;
                }

                if (psMetrics.isMeshRenderMode && renderer.mesh != null)
                    psMetrics.meshAssetPath = AssetDatabase.GetAssetPath(renderer.mesh);
            }
        }

        static void CollectFromRenderer(
            Renderer renderer,
            HashSet<int> seen,
            List<VfxMaterialMetrics> materials,
            List<string> referencedAssetPaths)
        {
            if (renderer == null)
                return;

            AddMaterials(renderer.sharedMaterials, seen, materials, referencedAssetPaths);

            var psr = renderer as ParticleSystemRenderer;
            if (psr != null && psr.trailMaterial != null)
                AddMaterial(psr.trailMaterial, seen, materials, referencedAssetPaths);
        }

        static void AddMaterials(
            Material[] mats,
            HashSet<int> seen,
            List<VfxMaterialMetrics> materials,
            List<string> referencedAssetPaths)
        {
            if (mats == null)
                return;
            for (int i = 0; i < mats.Length; i++)
                AddMaterial(mats[i], seen, materials, referencedAssetPaths);
        }

        static void AddMaterial(
            Material material,
            HashSet<int> seen,
            List<VfxMaterialMetrics> materials,
            List<string> referencedAssetPaths)
        {
            if (material == null)
                return;

            int id = material.GetInstanceID();
            if (!seen.Add(id))
                return;

            string path = AssetDatabase.GetAssetPath(material);
            AddRef(referencedAssetPaths, path);

            Shader shader = material.shader;
            VfxShaderRiskInfo risk = VfxShaderRiskAnalyzer.Analyze(shader);
            string shaderPath = shader != null ? AssetDatabase.GetAssetPath(shader) : string.Empty;
            AddRef(referencedAssetPaths, shaderPath);

            materials.Add(new VfxMaterialMetrics
            {
                assetPath = path,
                materialName = material.name,
                shaderName = shader != null ? shader.name : string.Empty,
                shaderAssetPath = shaderPath,
                keywords = material.shaderKeywords ?? System.Array.Empty<string>(),
                hasGrabPass = risk.HasGrabPass,
                hasSoftParticles = VfxShaderRiskAnalyzer.MaterialHasSoftParticles(material, risk),
                hasDistortion = VfxShaderRiskAnalyzer.MaterialHasDistortion(material, risk),
                renderQueue = material.renderQueue
            });
        }

        static void AddRef(List<string> referencedAssetPaths, string path)
        {
            if (string.IsNullOrEmpty(path) || referencedAssetPaths.Contains(path))
                return;
            referencedAssetPaths.Add(path);
        }
    }
}
