using System;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    [Serializable]
    public class VfxTypeBudget
    {
        public VfxEffectType type;
        public string displayName;
        public string matchingLabel;

        [Header("粒子")]
        public int maxDerivedParticles = 100;
        public int maxDerivedParticlesError = 200;
        public float maxParticlesRedundancyRatio = 2f;
        public int maxParticlesRedundancyAbsMin = 64;
        public int maxMeshTriangles = 100;
        public int maxMeshTrianglesError = 300;
        public bool forbidCollision = true;
        public bool forbidTrigger = true;
        public bool forbidLights = true;
        public bool warnNoise = true;
        public bool warnSubEmitters = true;
        public bool warnWorldSpace = true;

        [Header("材质与渲染")]
        public int maxMaterialCount = 4;
        public int maxMaterialCountError = 8;

        [Header("贴图")]
        public int maxTextureSize = 512;
        public int maxTextureSizeError = 1024;
        public bool requirePot = true;
        public bool forbidUncompressedTexture = true;
        public VfxMipmapPolicy mipmapPolicy = VfxMipmapPolicy.Any;

        [Header("层级")]
        public int maxHierarchyDepth = 6;
        public int maxHierarchyDepthError = 10;
        public int maxNodeCount = 20;
        public int maxNodeCountError = 40;
    }

    [Serializable]
    public class VfxPathMatchRule
    {
        public VfxEffectType type = VfxEffectType.Default;
        public string pathContains = string.Empty;
    }
}
