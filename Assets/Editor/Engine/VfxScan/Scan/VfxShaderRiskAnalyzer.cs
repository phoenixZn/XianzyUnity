using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public readonly struct VfxShaderRiskInfo
    {
        public readonly bool HasGrabPass;
        public readonly bool HasSoftParticles;
        public readonly bool HasDistortion;

        public VfxShaderRiskInfo(bool hasGrabPass, bool hasSoftParticles, bool hasDistortion)
        {
            HasGrabPass = hasGrabPass;
            HasSoftParticles = hasSoftParticles;
            HasDistortion = hasDistortion;
        }
    }

    public static class VfxShaderRiskAnalyzer
    {
        static readonly Dictionary<int, VfxShaderRiskInfo> Cache = new Dictionary<int, VfxShaderRiskInfo>();

        static readonly string[] SoftParticlesTokens =
        {
            "GrabPass",
            "_SOFTPARTICLES",
            "SOFTPARTICLES_ON",
            "_CameraDepthTexture",
            "SAMPLE_DEPTH_TEXTURE",
            "SoftParticles"
        };

        static readonly string[] DistortionTokens =
        {
            "Distort",
            "Distortion",
            "Refract",
            "HeatDistortion",
            "ScreenDistort"
        };

        public static void ClearCache()
        {
            Cache.Clear();
        }

        public static VfxShaderRiskInfo Analyze(Shader shader)
        {
            if (shader == null)
                return default;

            int id = shader.GetInstanceID();
            if (Cache.TryGetValue(id, out VfxShaderRiskInfo cached))
                return cached;

            bool grabPass = false;
            bool soft = false;
            bool distort = false;

            string shaderName = shader.name ?? string.Empty;
            if (ContainsAny(shaderName, DistortionTokens))
                distort = true;
            if (ContainsAny(shaderName, new[] { "SoftParticle", "Soft Particle" }))
                soft = true;

            string path = AssetDatabase.GetAssetPath(shader);
            if (!string.IsNullOrEmpty(path) && File.Exists(path) &&
                (path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase) ||
                 path.EndsWith(".shadergraph", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    string text = File.ReadAllText(path);
                    if (ContainsToken(text, "GrabPass"))
                        grabPass = true;
                    if (ContainsAny(text, SoftParticlesTokens))
                        soft = true;
                    if (ContainsAny(text, DistortionTokens))
                        distort = true;
                }
                catch
                {
                    // 读不到源文件时仅依赖 shader 名与材质 keyword。
                }
            }

            var info = new VfxShaderRiskInfo(grabPass, soft, distort);
            Cache[id] = info;
            return info;
        }

        public static bool MaterialHasSoftParticles(Material material, VfxShaderRiskInfo shaderInfo)
        {
            if (shaderInfo.HasSoftParticles)
                return true;
            if (material == null)
                return false;
            return HasKeyword(material, "_SOFTPARTICLES_ON") ||
                   HasKeyword(material, "SOFTPARTICLES_ON") ||
                   HasKeyword(material, "_SOFTPARTICLES");
        }

        public static bool MaterialHasDistortion(Material material, VfxShaderRiskInfo shaderInfo)
        {
            if (shaderInfo.HasDistortion)
                return true;
            if (material == null)
                return false;
            string[] keywords = material.shaderKeywords;
            if (keywords == null)
                return false;
            for (int i = 0; i < keywords.Length; i++)
            {
                if (ContainsAny(keywords[i], DistortionTokens))
                    return true;
            }

            return false;
        }

        static bool HasKeyword(Material material, string keyword)
        {
            if (material.IsKeywordEnabled(keyword))
                return true;
            string[] keywords = material.shaderKeywords;
            if (keywords == null)
                return false;
            for (int i = 0; i < keywords.Length; i++)
            {
                if (string.Equals(keywords[i], keyword, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        static bool ContainsToken(string text, string token)
        {
            return !string.IsNullOrEmpty(text) &&
                   text.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static bool ContainsAny(string text, string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null)
                return false;
            for (int i = 0; i < tokens.Length; i++)
            {
                if (ContainsToken(text, tokens[i]))
                    return true;
            }

            return false;
        }
    }
}
