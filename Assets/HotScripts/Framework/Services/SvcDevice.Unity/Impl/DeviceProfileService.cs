using System;
using System.Collections.Generic;
using System.Linq;
using Framework;
using UnityEngine;
using YooAsset;

namespace Xease
{
    /// <summary>
    /// 硬件性能适配服务。根据 CPU/GPU 规则匹配分数，划分挡位，应用功能开关。
    /// 配置以 JSON 存储，经资源服务加载，支持热更新。
    /// </summary>
    public class DeviceProfileService : IDeviceProfileService
    {
        // 工程内 JSON 目录；YooAsset address 为同名无扩展文件名
        public const string DeviceProfilePath = "Assets/HotAssets/Config/Engine/DeviceProfile";

        private readonly DeviceProfileApplier _applier = new DeviceProfileApplier();
        private bool _initialized;
        private ResolvedTierSwitches _resolvedSwitches;
        private DeviceProfileConfigDto _cachedConfig;
        private int _hardwareScore;
        // 硬件评分选档的档名；SetTierByName 不改
        private string _hardwareTierName;
        private bool _tierOverridden;

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        /// <summary>
        /// 还原已应用的开关并清空运行时状态。
        /// </summary>
        public void Shutdown()
        {
            _applier.Cleanup();
            _resolvedSwitches = null;
            _cachedConfig = null;
            _hardwareScore = 0;
            _hardwareTierName = null;
            _tierOverridden = false;
            _initialized = false;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IDeviceProfileService:
        public string CurrentTierName => _resolvedSwitches?.TierName;
        public int CurrentTierIndex => ResolveTierIndex(CurrentTierName);
        public int CurrentScore => _resolvedSwitches?.Score ?? 0;
        public int CurrentHardwareScore => _hardwareScore;
        public string HardwareTierName => _hardwareTierName;
        public bool IsInitialized => _initialized;
        public bool IsTierOverridden => _tierOverridden;

        public bool SetTierByName(string tierName)
        {
            if (!_initialized || _cachedConfig == null)
            {
                G.LogWarning("[DeviceProfile] SetTierByName: not initialized.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(tierName))
            {
                G.LogWarning("[DeviceProfile] SetTierByName: tierName is empty.");
                return false;
            }

            tierName = tierName.Trim();
            if (!TierExistsInConfig(tierName, _cachedConfig))
            {
                G.LogWarning($"[DeviceProfile] SetTierByName: unknown tier '{tierName}'.");
                return false;
            }

            var resolved = TierProfileResolverHelper.ResolveSwitches(tierName, _hardwareScore, _cachedConfig);
            _resolvedSwitches = resolved;
            _tierOverridden = true;
            ApplyResolvedProfile(resolved);
            G.Log($"[DeviceProfile] Tier overridden -> {tierName} (hardware score={_hardwareScore})");
            return true;
        }

        public bool RestoreHardwareTier()
        {
            if (!_initialized || _cachedConfig == null)
            {
                G.LogWarning("[DeviceProfile] RestoreHardwareTier: not initialized.");
                return false;
            }

            if (!_tierOverridden)
            {
                G.Log("[DeviceProfile] RestoreHardwareTier: tier not overridden, skip.");
                return false;
            }

            var resolved = TierProfileResolverHelper.Resolve(_cachedConfig, _hardwareScore);
            if (resolved == null)
            {
                G.LogWarning("[DeviceProfile] RestoreHardwareTier: resolve failed.");
                return false;
            }

            _resolvedSwitches = resolved;
            _tierOverridden = false;
            ApplyResolvedProfile(resolved);
            G.Log($"[DeviceProfile] Restored hardware tier -> {resolved.TierName}, score={_hardwareScore}");
            return true;
        }

        public int ResolveTierIndex(string tierName)
        {
            if (string.IsNullOrEmpty(tierName))
            {
                return 0;
            }

            const string prefix = "Tier";
            if (!tierName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var suffix = tierName.Substring(prefix.Length);
            return int.TryParse(suffix, out var index) ? index : 0;
        }

        public IReadOnlyList<string> GetAvailableTiers()
        {
            if (!_initialized || _cachedConfig?.tierConfigs == null)
            {
                G.LogWarning("[DeviceProfile] GetAvailableTiers: not initialized.");
                return Array.Empty<string>();
            }

            var tiers = _cachedConfig.tierConfigs;
            var result = new string[tiers.Count];
            for (var i = 0; i < tiers.Count; i++)
            {
                result[i] = tiers[i].tierName;
            }

            return result;
        }

        public int GetRecommendedFps(string tierName)
        {
            if (!_initialized || _cachedConfig?.tierConfigs == null)
            {
                G.LogWarning("[DeviceProfile] GetRecommendedFps: not initialized.");
                return 0;
            }

            if (string.IsNullOrWhiteSpace(tierName))
            {
                return 0;
            }

            tierName = tierName.Trim();
            var tiers = _cachedConfig.tierConfigs;
            foreach (var t in tiers)
            {
                if (string.Equals(t.tierName, tierName, StringComparison.OrdinalIgnoreCase))
                {
                    return t.recommendedFps;
                }
            }

            return 0;
        }

        public void LogDeviceProfileInfo()
        {
            if (!_initialized)
            {
                G.Log("[DeviceProfile] 未初始化");
                return;
            }

            G.Log($"[DeviceProfile] Tier={CurrentTierName}, Score={CurrentScore}, Overridden={_tierOverridden}, GPU={SystemInfo.graphicsDeviceName}, CPU={SystemInfo.processorType}");
        }

        public void ConsoleSetTier(string tierName)
        {
            SetTierByName(tierName ?? "");
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>
        /// 使用指定配置初始化（可由外部传入已加载的配置）。
        /// </summary>
        public void Initialize(DeviceProfileConfigDto config)
        {
            if (config == null)
            {
                G.LogWarning("[DeviceProfile] Config is null, skip initialization.");
                return;
            }

            var platform = (RuntimePlatform)config.platform;
            if (platform != Application.platform && platform != RuntimePlatform.WindowsEditor)
            {
                G.Log($"[DeviceProfile] Config platform {platform} != current {Application.platform}, skip.");
                return;
            }

            RunInitialization(config);
        }

        /// <summary>
        /// 按当前运行平台加载 JSON 并初始化（支持 AssetBundle 热更新）。
        /// </summary>
        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            var platform = Application.platform;
            string assetName = platform switch
            {
                RuntimePlatform.WindowsPlayer => "WindowsDeviceProfile",
                RuntimePlatform.WindowsEditor => "WindowsDeviceProfile",
                RuntimePlatform.Android => "AndroidDeviceProfile",
                _ => null
            };

            if (string.IsNullOrEmpty(assetName))
            {
                G.Log($"[DeviceProfile] Platform {platform} not supported, skip.");
                return;
            }

            string assetPath = $"{DeviceProfilePath}/{assetName}.json";
            var handle = G.Asset.LoadAssetRawFileSync(assetName);
            if (handle.Status != EOperationStatus.Succeed)
            {
                G.LogWarning($"[DeviceProfile] Config not found: {assetPath}, {handle.LastError}");
                return;
            }

            var str = handle.GetRawFileText();
            if (string.IsNullOrEmpty(str))
            {
                G.LogWarning($"[DeviceProfile] Invalid config: {assetPath}");
                return;
            }

            DeviceProfileConfigDto config;
            try
            {
                config = JsonHelper.FromJsonNewtonsoft<DeviceProfileConfigDto>(str);
            }
            catch (Exception e)
            {
                G.LogWarning($"[DeviceProfile] JSON parse error: {assetPath}, {e.Message}");
                return;
            }

            if (config == null)
            {
                G.LogWarning($"[DeviceProfile] Invalid config type: {assetPath}");
                return;
            }

            G.Log($"[DeviceProfile] Loaded config: {assetPath}");
            RunInitialization(config);
        }

#if UNITY_EDITOR
        [UnityEditor.MenuItem("Tools/DeviceProfile/Test Load Device Profile")]
        private static void TestLoadDeviceProfile()
        {
            string assetPath = $"{DeviceProfilePath}/AndroidDeviceProfile.json";
            var textAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (textAsset == null || string.IsNullOrEmpty(textAsset.text))
            {
                G.LogWarning($"[DeviceProfile] Invalid config: {assetPath}");
                return;
            }

            DeviceProfileConfigDto config;
            try
            {
                config = JsonHelper.FromJsonNewtonsoft<DeviceProfileConfigDto>(textAsset.text);
            }
            catch (Exception e)
            {
                G.LogWarning($"[DeviceProfile] JSON parse error: {assetPath}, {e.Message}");
                return;
            }

            if (config == null)
            {
                G.LogWarning($"[DeviceProfile] Invalid config type: {assetPath}");
                return;
            }

            G.Log($"[DeviceProfile] Loaded config: {assetPath}");
        }
#endif

        private void RunInitialization(DeviceProfileConfigDto config)
        {
            if (_initialized)
            {
                _applier.Cleanup();
            }

            var (gpuName, cpuName, processorFrequencyMHz) = ScoreRuleEvaluatorHelper.GatherHardwareInfo();
            int score = ScoreRuleEvaluatorHelper.Evaluate(config.scoreRules, gpuName, cpuName, processorFrequencyMHz);
            var resolved = TierProfileResolverHelper.Resolve(config, score);
            if (resolved == null)
            {
                G.LogWarning("[DeviceProfile] Failed to resolve tier profile.");
                return;
            }

            _cachedConfig = config;
            _hardwareScore = score;
            _hardwareTierName = resolved.TierName;
            _tierOverridden = false;
            _resolvedSwitches = resolved;
            _initialized = true;

            G.Log($"[DeviceProfile] GPU={gpuName}, CPU={cpuName}, Freq={processorFrequencyMHz}MHz, Score={score}, Tier={resolved.TierName}");
            ApplyResolvedProfile(resolved);
        }

        private void ApplyResolvedProfile(ResolvedTierSwitches resolved)
        {
            _applier.Apply(resolved);
        }

        private static bool TierExistsInConfig(string tierName, DeviceProfileConfigDto config)
        {
            return config?.tierConfigs != null &&
                   config.tierConfigs.Any(t => string.Equals(t.tierName, tierName, StringComparison.OrdinalIgnoreCase));
        }
    }
}
