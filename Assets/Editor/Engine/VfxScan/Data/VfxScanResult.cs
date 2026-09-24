using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    [Serializable]
    public class VfxScanResult
    {
        public VfxPrefabMetrics metrics = new VfxPrefabMetrics();
        public VfxEffectType resolvedType;
        public string budgetDisplayName;
        public int budgetMaxParticles;
        public int budgetMaxMaterials;
        public int budgetMaxTextureSize;
        public VfxHealthStatus status;
        public List<VfxScanIssue> issues = new List<VfxScanIssue>();

        public string PrefabPath => metrics != null ? metrics.prefabPath : string.Empty;
        public string PrefabName => metrics != null ? metrics.prefabName : string.Empty;
        public int MaterialCount => metrics != null && metrics.materials != null ? metrics.materials.Count : 0;
        public int ParticleCount => metrics != null && metrics.particleSystems != null ? metrics.particleSystems.Count : 0;
        public int TextureCount => metrics != null && metrics.textures != null ? metrics.textures.Count : 0;
        public int TotalDerivedParticles => metrics != null ? metrics.totalDerivedParticles : 0;

        public string RiskModuleSummary
        {
            get
            {
                if (metrics == null || metrics.particleSystems == null)
                    return "无";

                var names = new List<string>();
                for (int i = 0; i < metrics.particleSystems.Count; i++)
                {
                    VfxParticleSystemMetrics ps = metrics.particleSystems[i];
                    AddUnique(names, ps.collisionEnabled, "Collision");
                    AddUnique(names, ps.triggerEnabled, "Trigger");
                    AddUnique(names, ps.lightsEnabled, "Lights");
                    AddUnique(names, ps.noiseEnabled, "Noise");
                    AddUnique(names, ps.subEmittersEnabled, "SubEmitters");
                }

                return names.Count == 0 ? "无" : string.Join(" / ", names);
            }
        }

        public bool HasError
        {
            get
            {
                if (issues == null)
                    return false;
                for (int i = 0; i < issues.Count; i++)
                {
                    if (issues[i].severity == VfxHealthStatus.Error)
                        return true;
                }

                return false;
            }
        }

        static void AddUnique(List<string> names, bool enabled, string label)
        {
            if (!enabled || names.Contains(label))
                return;
            names.Add(label);
        }
    }
}
