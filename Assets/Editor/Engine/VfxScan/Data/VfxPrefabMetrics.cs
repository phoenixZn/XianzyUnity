using System;
using System.Collections.Generic;
using UnityEngine;

namespace Script.Editor.Extended.Engine.VfxScan
{
    [Serializable]
    public class VfxParticleSystemMetrics
    {
        public string hierarchyPath;
        public string objectName;
        public bool componentEnabled;
        public bool gameObjectActive;
        public int maxParticles;
        public int derivedParticles;
        public float rateOverTimeMax;
        public float lifetimeMax;
        public float duration;
        public bool looping;
        public int burstSum;
        public bool collisionEnabled;
        public bool triggerEnabled;
        public bool noiseEnabled;
        public bool lightsEnabled;
        public bool subEmittersEnabled;
        public ParticleSystemSimulationSpace simulationSpace;
        public bool isMeshRenderMode;
        public int meshTriangleCount;
        public string meshName;
        public string meshAssetPath;
        public string materialAssetPath;
        public string sortingLayer;
        public int sortingOrder;
        public int renderQueue;
    }

    [Serializable]
    public class VfxMaterialMetrics
    {
        public string assetPath;
        public string materialName;
        public string shaderName;
        public string shaderAssetPath;
        public string[] keywords;
        public bool hasGrabPass;
        public bool hasSoftParticles;
        public bool hasDistortion;
        public int renderQueue;
    }

    [Serializable]
    public class VfxTextureMetrics
    {
        public string assetPath;
        public string textureName;
        public int width;
        public int height;
        public bool isPot;
        public bool mipmapEnabled;
        public string androidFormat;
        public string iosFormat;
        public bool isUncompressed;
        public bool importerFound;
    }

    [Serializable]
    public class VfxRedundantComponentInfo
    {
        public string hierarchyPath;
        public string componentType;
        public string reason;
    }

    [Serializable]
    public class VfxHierarchyMetrics
    {
        public int nodeCount;
        public int maxDepth;
        public List<VfxRedundantComponentInfo> redundants = new List<VfxRedundantComponentInfo>();
    }

    [Serializable]
    public class VfxPrefabMetrics
    {
        public string prefabPath;
        public string prefabName;
        public string assetGuid;
        public string[] labels = Array.Empty<string>();
        public int totalDerivedParticles;
        public int maxTextureWidth;
        public int maxTextureHeight;
        public List<VfxParticleSystemMetrics> particleSystems = new List<VfxParticleSystemMetrics>();
        public List<VfxMaterialMetrics> materials = new List<VfxMaterialMetrics>();
        public List<VfxTextureMetrics> textures = new List<VfxTextureMetrics>();
        public VfxHierarchyMetrics hierarchy = new VfxHierarchyMetrics();
        public List<string> referencedAssetPaths = new List<string>();
    }
}
