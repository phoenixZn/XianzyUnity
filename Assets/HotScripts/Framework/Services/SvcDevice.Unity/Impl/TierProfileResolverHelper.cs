using System;
using System.Collections.Generic;
using System.Linq;

namespace Xease
{
    /// <summary>
    /// 挡位继承解析：总分 → 挡位名 → 合并父级+子级开关（基于合并后的 TierConfigDto）。
    /// </summary>
    public static class TierProfileResolverHelper
    {
        /// <summary>
        /// 根据分数解析挡位并合并开关。
        /// </summary>
        public static ResolvedTierSwitches Resolve(DeviceProfileConfigDto config, int score)
        {
            var tierName = ResolveTierName(score, config);
            if (string.IsNullOrEmpty(tierName))
            {
                return null;
            }

            return ResolveSwitches(tierName, score, config);
        }

        /// <summary>
        /// 根据分数从 tierConfigs 中解析挡位名（按 maxScore 升序，取第一个 score &lt; maxScore）。
        /// 无匹配（score == 0）落到 maxScore 最高的挡位。
        /// </summary>
        public static string ResolveTierName(int score, DeviceProfileConfigDto config)
        {
            if (config?.tierConfigs == null || config.tierConfigs.Count == 0)
            {
                return null;
            }

            var sorted = config.tierConfigs.OrderBy(t => t.maxScore).ToList();
            if (score == 0)
            {
                return sorted[sorted.Count - 1].tierName;
            }

            foreach (var t in sorted)
            {
                if (score < t.maxScore)
                {
                    return t.tierName;
                }
            }

            return sorted[sorted.Count - 1].tierName;
        }

        /// <summary>
        /// 根据挡位名解析继承链并合并开关。先写入与 <see cref="TierSwitchOverridesDto"/> 一致的基线，再按继承链覆盖，避免 null 导致 Applier 跳过而保留上一挡位状态。
        /// </summary>
        public static ResolvedTierSwitches ResolveSwitches(string tierName, int score, DeviceProfileConfigDto config)
        {
            var result = new ResolvedTierSwitches { TierName = tierName, Score = score };
            TierSwitchOverridesDto.CopyDefaultEffectiveValuesToResolved(result);
            if (config?.tierConfigs == null)
            {
                return result;
            }

            var profileMap = config.tierConfigs.ToDictionary(p => p.tierName, StringComparer.OrdinalIgnoreCase);
            if (!profileMap.TryGetValue(tierName, out var current))
            {
                return result;
            }

            var chain = new List<TierConfigDto>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var cur = current;
            while (cur != null)
            {
                if (visited.Contains(cur.tierName))
                {
                    break;
                }

                visited.Add(cur.tierName);
                chain.Add(cur);
                if (string.IsNullOrEmpty(cur.baseTierName) || !profileMap.TryGetValue(cur.baseTierName, out cur))
                {
                    break;
                }
            }

            chain.Reverse();
            foreach (var p in chain)
            {
                MergeOverrides(result, p.switches);
            }

            return result;
        }

        private static void MergeOverrides(ResolvedTierSwitches target, TierSwitchOverridesDto overrides)
        {
            if (overrides == null)
            {
                return;
            }

            if (overrides.overrideShadowEnabled)
            {
                target.ShadowEnabled = overrides.shadowEnabled;
            }

            if (overrides.overridePostProcessEnabled)
            {
                target.PostProcessEnabled = overrides.postProcessEnabled;
            }

            if (overrides.overrideShadowDistance)
            {
                target.ShadowDistance = overrides.shadowDistance;
            }

            if (overrides.overrideAntiAliasing)
            {
                target.AntiAliasing = overrides.antiAliasing;
            }

            if (overrides.overrideShaderTier)
            {
                target.ShaderTier = overrides.shaderTier;
            }

            if (overrides.overrideOutlineEnabled)
            {
                target.OutlineEnabled = overrides.outlineEnabled;
            }

            if (overrides.overrideVisualEffectLOD)
            {
                target.VisualEffectLOD = overrides.visualEffectLOD;
            }
        }
    }
}
