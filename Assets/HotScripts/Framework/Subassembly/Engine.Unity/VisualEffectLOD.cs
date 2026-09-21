using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xease.Engine
{
    [Serializable]
    public class NodeOverride
    {
        public GameObject target;
        public bool isActive = true;
    }

    [Serializable]
    public class ParticleOverride
    {
        public ParticleSystem target;
        public float emissionMultiplier = 1f;
        public float maxParticlesMultiplier = 1f;
    }

    [Serializable]
    public class VisualEffectLodTier
    {
        public List<NodeOverride> nodeOverrides = new List<NodeOverride>();
        public List<ParticleOverride> particleOverrides = new List<ParticleOverride>();
    }

    public class VisualEffectLOD : MonoBehaviour
    {
        public List<VisualEffectLodTier> lodTiers = new List<VisualEffectLodTier>();

        private struct NodeBaseState
        {
            public GameObject target;
            public bool isActive;
        }

        private struct ParticleBaseState
        {
            public ParticleSystem target;
            public float emissionRateOverTime;
            public int maxParticles;
        }

        private static readonly HashSet<VisualEffectLOD> s_ActiveInstances = new HashSet<VisualEffectLOD>();
        private static readonly List<VisualEffectLOD> s_ApplyBuffer = new List<VisualEffectLOD>(64);
        private static int s_GlobalLevel;

        private readonly List<NodeBaseState> _nodeBaseStates = new List<NodeBaseState>(16);
        private readonly List<ParticleBaseState> _particleBaseStates = new List<ParticleBaseState>(16);
        private readonly Dictionary<GameObject, int> _nodeIndexMap = new Dictionary<GameObject, int>(16);
        private readonly Dictionary<ParticleSystem, int> _particleIndexMap = new Dictionary<ParticleSystem, int>(16);
        private bool _isBaseCacheReady;

        public static int GlobalLevel => s_GlobalLevel;

        private void OnEnable()
        {
            s_ActiveInstances.Add(this);
            EnsureBaseCache();
            ApplyLOD(s_GlobalLevel);
        }

        private void OnDisable()
        {
            s_ActiveInstances.Remove(this);
        }

        public static void SetGlobalLevel(int level)
        {
            s_GlobalLevel = Mathf.Max(0, level);
            if (s_ActiveInstances.Count <= 0)
            {
                return;
            }

            s_ApplyBuffer.Clear();
            foreach (var instance in s_ActiveInstances)
            {
                s_ApplyBuffer.Add(instance);
            }

            for (int i = 0; i < s_ApplyBuffer.Count; i++)
            {
                VisualEffectLOD instance = s_ApplyBuffer[i];
                if (instance != null)
                {
                    instance.ApplyLOD(s_GlobalLevel);
                }
            }

            s_ApplyBuffer.Clear();
        }

        public void ApplyLOD(int level)
        {
            EnsureBaseCache();
            RestoreBaseState();

            int tierIndex = ResolveTierIndex(level);
            if (tierIndex < 0)
            {
                return;
            }

            if (lodTiers == null || tierIndex >= lodTiers.Count)
            {
                return;
            }

            VisualEffectLodTier tier = lodTiers[tierIndex];
            if (tier == null)
            {
                return;
            }

            ApplyNodeOverrides(tier);
            ApplyParticleOverrides(tier);
        }

        /// <summary>
        /// 失效并重建 Base 缓存。若已有缓存，会先按旧缓存把场景恢复到 Base（避免在仍处于 LOD 预览态时用
        /// <see cref="GameObject.activeSelf"/> 误采样为 Base）。
        /// </summary>
        public void RefreshBaseCache()
        {
            if (_isBaseCacheReady)
            {
                RestoreBaseState();
            }

            InvalidateBaseCache();
            EnsureBaseCache();
        }

        public void InvalidateBaseCache()
        {
            _isBaseCacheReady = false;
        }

        public void RestoreBaseForEditor()
        {
            EnsureBaseCache();
            RestoreBaseState();
        }

        /// <summary>
        /// 用当前场景中各 override 目标的即时状态重建内存 Base 缓存（不先 Restore）。
        /// 仅编辑器捕获 Base 时调用，数据不序列化。
        /// </summary>
        public void RebuildBaseCacheFromCurrentScene()
        {
            RebuildBaseCacheInternal();
        }

        public bool IsBaseCacheReady => _isBaseCacheReady;

        public void ForEachBaseNodeState(System.Action<GameObject, bool> action)
        {
            if (action == null)
            {
                return;
            }

            for (int i = 0; i < _nodeBaseStates.Count; i++)
            {
                NodeBaseState baseState = _nodeBaseStates[i];
                if (baseState.target == null)
                {
                    continue;
                }

                action(baseState.target, baseState.isActive);
            }
        }

        public void ForEachBaseParticleState(System.Action<ParticleSystem, float, int> action)
        {
            if (action == null)
            {
                return;
            }

            for (int i = 0; i < _particleBaseStates.Count; i++)
            {
                ParticleBaseState baseState = _particleBaseStates[i];
                if (baseState.target == null)
                {
                    continue;
                }

                action(baseState.target, baseState.emissionRateOverTime, baseState.maxParticles);
            }
        }

        private int ResolveTierIndex(int level)
        {
            if (level <= 0)
            {
                return -1;
            }

            if (lodTiers == null || lodTiers.Count == 0)
            {
                return -1;
            }

            int requestedTierIndex = level - 1;
            if (requestedTierIndex < 0)
            {
                return -1;
            }

            if (requestedTierIndex >= lodTiers.Count)
            {
                return lodTiers.Count - 1;
            }

            return requestedTierIndex;
        }

        private void EnsureBaseCache()
        {
            if (_isBaseCacheReady)
            {
                return;
            }

            RebuildBaseCacheInternal();
        }

        private void RebuildBaseCacheInternal()
        {
            _nodeBaseStates.Clear();
            _particleBaseStates.Clear();
            _nodeIndexMap.Clear();
            _particleIndexMap.Clear();

            if (lodTiers != null)
            {
                for (int tierIndex = 0; tierIndex < lodTiers.Count; tierIndex++)
                {
                    CollectBaseStatesFromTier(lodTiers[tierIndex]);
                }
            }

            _isBaseCacheReady = true;
        }

        private void CollectBaseStatesFromTier(VisualEffectLodTier tier)
        {
            if (tier == null)
            {
                return;
            }

            if (tier.nodeOverrides != null)
            {
                for (int nodeIndex = 0; nodeIndex < tier.nodeOverrides.Count; nodeIndex++)
                {
                    NodeOverride nodeOverride = tier.nodeOverrides[nodeIndex];
                    if (nodeOverride == null || nodeOverride.target == null)
                    {
                        continue;
                    }

                    if (_nodeIndexMap.ContainsKey(nodeOverride.target))
                    {
                        continue;
                    }

                    NodeBaseState baseState;
                    baseState.target = nodeOverride.target;
                    baseState.isActive = nodeOverride.target.activeSelf;
                    _nodeIndexMap.Add(nodeOverride.target, _nodeBaseStates.Count);
                    _nodeBaseStates.Add(baseState);
                }
            }

            if (tier.particleOverrides != null)
            {
                for (int particleIndex = 0; particleIndex < tier.particleOverrides.Count; particleIndex++)
                {
                    ParticleOverride particleOverride = tier.particleOverrides[particleIndex];
                    if (particleOverride == null || particleOverride.target == null)
                    {
                        continue;
                    }

                    if (_particleIndexMap.ContainsKey(particleOverride.target))
                    {
                        continue;
                    }

                    ParticleSystem targetParticle = particleOverride.target;
                    ParticleSystem.EmissionModule emission = targetParticle.emission;
                    ParticleSystem.MainModule main = targetParticle.main;

                    ParticleBaseState baseState;
                    baseState.target = targetParticle;
                    baseState.emissionRateOverTime = emission.rateOverTimeMultiplier;
                    baseState.maxParticles = main.maxParticles;
                    _particleIndexMap.Add(targetParticle, _particleBaseStates.Count);
                    _particleBaseStates.Add(baseState);
                }
            }
        }

        private void RestoreBaseState()
        {
            for (int i = 0; i < _nodeBaseStates.Count; i++)
            {
                NodeBaseState baseState = _nodeBaseStates[i];
                if (baseState.target == null)
                {
                    continue;
                }

                baseState.target.SetActive(baseState.isActive);
                // 编辑器模式下主动调用一下发射，否则不会显示
#if UNITY_EDITOR
                if (baseState.isActive)
                {
                    baseState.target.GetComponent<ParticleSystem>().Play(true);
                }
#endif
            }

            for (int i = 0; i < _particleBaseStates.Count; i++)
            {
                ParticleBaseState baseState = _particleBaseStates[i];
                if (baseState.target == null)
                {
                    continue;
                }

                ParticleSystem.EmissionModule emission = baseState.target.emission;
                ParticleSystem.MainModule main = baseState.target.main;
                emission.rateOverTimeMultiplier = baseState.emissionRateOverTime;
                main.maxParticles = baseState.maxParticles;
            }
        }

        private void ApplyNodeOverrides(VisualEffectLodTier tier)
        {
            if (tier.nodeOverrides == null)
            {
                return;
            }

            for (int i = 0; i < tier.nodeOverrides.Count; i++)
            {
                NodeOverride nodeOverride = tier.nodeOverrides[i];
                if (nodeOverride == null || nodeOverride.target == null)
                {
                    continue;
                }

                nodeOverride.target.SetActive(nodeOverride.isActive);
            }
        }

        private void ApplyParticleOverrides(VisualEffectLodTier tier)
        {
            if (tier.particleOverrides == null)
            {
                return;
            }

            for (int i = 0; i < tier.particleOverrides.Count; i++)
            {
                ParticleOverride particleOverride = tier.particleOverrides[i];
                if (particleOverride == null || particleOverride.target == null)
                {
                    continue;
                }

                if (!_particleIndexMap.TryGetValue(particleOverride.target, out int baseIndex))
                {
                    continue;
                }

                if (baseIndex < 0 || baseIndex >= _particleBaseStates.Count)
                {
                    continue;
                }

                ParticleBaseState baseState = _particleBaseStates[baseIndex];
                ParticleSystem.EmissionModule emission = particleOverride.target.emission;
                ParticleSystem.MainModule main = particleOverride.target.main;

                float emissionMultiplier = Mathf.Max(0f, particleOverride.emissionMultiplier);
                float maxParticlesMultiplier = Mathf.Max(0f, particleOverride.maxParticlesMultiplier);
                emission.rateOverTimeMultiplier = baseState.emissionRateOverTime * emissionMultiplier;
                main.maxParticles = Mathf.Max(0, Mathf.RoundToInt(baseState.maxParticles * maxParticlesMultiplier));
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void OnRuntimeInitialize()
        {
            s_ActiveInstances.Clear();
            s_ApplyBuffer.Clear();
            s_GlobalLevel = 0;
        }
#endif
    }
}
