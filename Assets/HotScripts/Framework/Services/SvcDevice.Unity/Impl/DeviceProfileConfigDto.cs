using System;
using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 规则匹配源类型（JSON 用 int：0=GPU, 1=CPU, 2=CPUFrequency）
    /// </summary>
    public enum ScoreSourceTypeDto
    {
        GPU = 0,
        CPU = 1,
        CPUFrequency = 2 // processorFrequency (MHz)，支持 matchString 前缀：>=、<=、>、<
    }

    /// <summary>
    /// 单条匹配条件（GPU/CPU 用正则，CPUFrequency 用数值前缀如 &gt;=、&lt;=、&gt;、&lt;）
    /// </summary>
    [Serializable]
    public class ScoreRuleMatchItemDto
    {
        public int sourceType; // 0=GPU, 1=CPU, 2=CPUFrequency
        public string matchString;
    }

    /// <summary>
    /// 分数规则 DTO。多条 matchItems 需同时匹配，匹配则返回 score；按配置顺序优先匹配，先匹配先返回。
    /// 兼容旧格式：matchItems 为空时，若 sourceType/matchString 存在则视为单条件规则
    /// </summary>
    [Serializable]
    public class ScoreRuleDto
    {
        public List<ScoreRuleMatchItemDto> matchItems = new List<ScoreRuleMatchItemDto>();
        public int score;
        // 兼容旧格式，matchItems 为空时使用
        public int sourceType = -1;
    }

    /// <summary>
    /// 挡位开关覆盖项 DTO
    /// </summary>
    [Serializable]
    public class TierSwitchOverridesDto
    {
        /// <summary>
        /// 将「未逐条 override」时的有效值写入 Resolved，与当前字段默认值一致；修改本类字段默认值时基线自动同步。
        /// </summary>
        public static void CopyDefaultEffectiveValuesToResolved(ResolvedTierSwitches target)
        {
            if (target == null)
            {
                return;
            }

            var d = new TierSwitchOverridesDto();
            target.ShadowEnabled = d.shadowEnabled;
            target.PostProcessEnabled = d.postProcessEnabled;
            target.ShadowDistance = d.shadowDistance;
            target.AntiAliasing = d.antiAliasing;
            target.ShaderTier = d.shaderTier;
            target.OutlineEnabled = d.outlineEnabled;
            target.VisualEffectLOD = d.visualEffectLOD;
        }

        public bool overrideShadowEnabled = false;
        public bool shadowEnabled = true;
        public bool overridePostProcessEnabled = false;
        public bool postProcessEnabled = true;
        public bool overrideShadowDistance = false;
        public float shadowDistance = 150f;
        public bool overrideAntiAliasing = false;
        public int antiAliasing = 4;
        public bool overrideShaderTier = false;
        public int shaderTier = 2;
        public bool overrideOutlineEnabled = false;
        public bool outlineEnabled = true;

        public bool overrideVisualEffectLOD = false;
        public int visualEffectLOD = 0;
    }

    /// <summary>
    /// 挡位配置 DTO（合并 ScoreThreshold + TierProfile）。
    /// maxScore: 分数上界，score &lt; maxScore 时属于该挡位。
    /// baseTierName: 父挡位，留空表示无父级。
    /// recommendedFps: 该挡位推荐目标帧率（供设置页展示 / 业务设置 targetFrameRate）。
    /// </summary>
    [Serializable]
    public class TierConfigDto
    {
        public string tierName;
        public int maxScore;
        public string baseTierName;
        public int recommendedFps;
        public TierSwitchOverridesDto switches = new TierSwitchOverridesDto();
    }

    /// <summary>
    /// 平台设备配置 DTO（JSON 格式，支持热更新）
    /// </summary>
    [Serializable]
    public class DeviceProfileConfigDto
    {
        public int platform; // RuntimePlatform 枚举值 2=WindowsPlayer, 8=iPhone, 11=Android
        public List<ScoreRuleDto> scoreRules = new List<ScoreRuleDto>();
        // 按 maxScore 升序排列，第一个 score < maxScore 的 tier 被选中
        public List<TierConfigDto> tierConfigs = new List<TierConfigDto>();
    }

    /// <summary>
    /// 合并后的挡位开关（用于应用）。解析时先经 <see cref="TierSwitchOverridesDto.CopyDefaultEffectiveValuesToResolved"/> 再合并继承链，字段均为有效值。
    /// </summary>
    public class ResolvedTierSwitches
    {
        public string TierName { get; set; }
        public int Score { get; set; }
        public bool ShadowEnabled { get; set; }
        public bool PostProcessEnabled { get; set; }
        public float ShadowDistance { get; set; }
        public int ShadowCascades { get; set; }
        public int AntiAliasing { get; set; }
        public int ShaderTier { get; set; }
        public bool OutlineEnabled { get; set; }
        public int VisualEffectLOD { get; set; }
    }
}
