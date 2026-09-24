using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxTextureCollector
    {
        static readonly HashSet<TextureImporterFormat> UncompressedFormats = new HashSet<TextureImporterFormat>
        {
            TextureImporterFormat.RGBA32,
            TextureImporterFormat.ARGB32,
            TextureImporterFormat.RGB24,
            TextureImporterFormat.Alpha8,
            TextureImporterFormat.R8,
            TextureImporterFormat.R16,
            TextureImporterFormat.RG16,
            TextureImporterFormat.RGBAFloat,
            TextureImporterFormat.RGBAHalf
        };

        public static void Collect(
            GameObject prefabRoot,
            List<VfxMaterialMetrics> materials,
            List<VfxTextureMetrics> textures,
            List<string> referencedAssetPaths)
        {
            var seen = new HashSet<int>();
            CollectFromRenderers(prefabRoot, seen, textures, referencedAssetPaths);

            if (materials == null)
                return;

            for (int i = 0; i < materials.Count; i++)
            {
                VfxMaterialMetrics matMetrics = materials[i];
                if (string.IsNullOrEmpty(matMetrics.assetPath))
                    continue;

                Material material = AssetDatabase.LoadAssetAtPath<Material>(matMetrics.assetPath);
                if (material == null)
                    continue;

                string[] texProps = material.GetTexturePropertyNames();
                for (int p = 0; p < texProps.Length; p++)
                {
                    Texture tex = material.GetTexture(texProps[p]);
                    AddTexture(tex, seen, textures, referencedAssetPaths);
                }
            }
        }

        static void CollectFromRenderers(
            GameObject prefabRoot,
            HashSet<int> seen,
            List<VfxTextureMetrics> textures,
            List<string> referencedAssetPaths)
        {
            if (prefabRoot == null)
                return;

            Renderer[] renderers = prefabRoot.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !VfxHierarchyPath.IsOwnedByPrefab(renderer.gameObject, prefabRoot))
                    continue;
                Material[] mats = renderer.sharedMaterials;
                if (mats == null)
                    continue;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null)
                        continue;
                    string[] texProps = mat.GetTexturePropertyNames();
                    for (int p = 0; p < texProps.Length; p++)
                        AddTexture(mat.GetTexture(texProps[p]), seen, textures, referencedAssetPaths);
                }
            }
        }

        static void AddTexture(
            Texture texture,
            HashSet<int> seen,
            List<VfxTextureMetrics> textures,
            List<string> referencedAssetPaths)
        {
            if (texture == null)
                return;
            if (texture is RenderTexture)
                return;
            if (!seen.Add(texture.GetInstanceID()))
                return;

            string path = AssetDatabase.GetAssetPath(texture);
            if (!string.IsNullOrEmpty(path))
            {
                if (!referencedAssetPaths.Contains(path))
                    referencedAssetPaths.Add(path);
            }

            var metrics = new VfxTextureMetrics
            {
                assetPath = path,
                textureName = texture.name,
                width = texture.width,
                height = texture.height,
                isPot = IsPowerOfTwo(texture.width) && IsPowerOfTwo(texture.height)
            };

            FillImporterInfo(metrics);
            textures.Add(metrics);
        }

        static void FillImporterInfo(VfxTextureMetrics metrics)
        {
            if (string.IsNullOrEmpty(metrics.assetPath))
                return;

            var importer = AssetImporter.GetAtPath(metrics.assetPath) as TextureImporter;
            if (importer == null)
                return;

            metrics.importerFound = true;
            metrics.mipmapEnabled = importer.mipmapEnabled;

            TextureImporterPlatformSettings android = importer.GetPlatformTextureSettings("Android");
            TextureImporterPlatformSettings ios = importer.GetPlatformTextureSettings("iPhone");
            metrics.androidFormat = DescribeFormat(importer, android);
            metrics.iosFormat = DescribeFormat(importer, ios);
            metrics.isUncompressed = IsUncompressed(importer, android) || IsUncompressed(importer, ios);
        }

        static string DescribeFormat(TextureImporter importer, TextureImporterPlatformSettings settings)
        {
            if (settings != null && settings.overridden)
                return settings.format.ToString();
            return importer.textureCompression.ToString();
        }

        static bool IsUncompressed(TextureImporter importer, TextureImporterPlatformSettings settings)
        {
            if (settings != null && settings.overridden)
                return UncompressedFormats.Contains(settings.format);

            return importer.textureCompression == TextureImporterCompression.Uncompressed;
        }

        static bool IsPowerOfTwo(int value)
        {
            return value > 0 && (value & (value - 1)) == 0;
        }
    }
}
