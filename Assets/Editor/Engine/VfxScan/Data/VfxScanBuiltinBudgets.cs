using System.Collections.Generic;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxScanBuiltinBudgets
    {
        public static List<VfxTypeBudget> CreateDefaultList()
        {
            return new List<VfxTypeBudget>
            {
                CreateDefault(),
                CreateUi(),
                CreateUltimate(),
                CreateScene()
            };
        }

        public static VfxTypeBudget Get(IReadOnlyList<VfxTypeBudget> budgets, VfxEffectType type)
        {
            if (budgets != null)
            {
                for (int i = 0; i < budgets.Count; i++)
                {
                    if (budgets[i] != null && budgets[i].type == type)
                        return budgets[i];
                }
            }

            switch (type)
            {
                case VfxEffectType.UI: return CreateUi();
                case VfxEffectType.Ultimate: return CreateUltimate();
                case VfxEffectType.Minion: return CreateMinion();
                case VfxEffectType.Scene: return CreateScene();
                default: return CreateDefault();
            }
        }

        public static VfxTypeBudget CreateDefault()
        {
            return new VfxTypeBudget
            {
                type = VfxEffectType.Default,
                displayName = "默认",
                matchingLabel = string.Empty,
                maxDerivedParticles = 80,
                maxDerivedParticlesError = 160,
                maxMaterialCount = 3,
                maxMaterialCountError = 6,
                maxTextureSize = 512,
                maxTextureSizeError = 1024,
                mipmapPolicy = VfxMipmapPolicy.MustOn
            };
        }

        public static VfxTypeBudget CreateUi()
        {
            return new VfxTypeBudget
            {
                type = VfxEffectType.UI,
                displayName = "UI 特效",
                matchingLabel = "VFX_UI",
                maxDerivedParticles = 40,
                maxDerivedParticlesError = 80,
                maxMaterialCount = 2,
                maxMaterialCountError = 4,
                maxTextureSize = 256,
                maxTextureSizeError = 512,
                maxNodeCount = 16,
                maxNodeCountError = 32,
                mipmapPolicy = VfxMipmapPolicy.MustOff
            };
        }

        public static VfxTypeBudget CreateUltimate()
        {
            return new VfxTypeBudget
            {
                type = VfxEffectType.Ultimate,
                displayName = "终极技能",
                matchingLabel = "VFX_Ultimate",
                maxDerivedParticles = 200,
                maxDerivedParticlesError = 300,
                maxMaterialCount = 8,
                maxMaterialCountError = 12,
                maxTextureSize = 512,
                maxTextureSizeError = 1024,
                mipmapPolicy = VfxMipmapPolicy.MustOn
            };
        }

        public static VfxTypeBudget CreateMinion()
        {
            return new VfxTypeBudget
            {
                type = VfxEffectType.Minion,
                displayName = "小怪 / 受击",
                matchingLabel = "VFX_Minion",
                maxDerivedParticles = 20,
                maxDerivedParticlesError = 40,
                maxMaterialCount = 1,
                maxMaterialCountError = 3,
                maxTextureSize = 256,
                maxTextureSizeError = 512,
                maxNodeCount = 12,
                maxNodeCountError = 24,
                mipmapPolicy = VfxMipmapPolicy.Any
            };
        }

        public static VfxTypeBudget CreateScene()
        {
            return new VfxTypeBudget
            {
                type = VfxEffectType.Scene,
                displayName = "场景常驻",
                matchingLabel = "VFX_Scene",
                maxDerivedParticles = 50,
                maxDerivedParticlesError = 100,
                maxMaterialCount = 2,
                maxMaterialCountError = 4,
                maxTextureSize = 512,
                maxTextureSizeError = 1024,
                forbidCollision = true,
                forbidTrigger = true,
                forbidLights = true,
                mipmapPolicy = VfxMipmapPolicy.MustOn
            };
        }
    }
}
