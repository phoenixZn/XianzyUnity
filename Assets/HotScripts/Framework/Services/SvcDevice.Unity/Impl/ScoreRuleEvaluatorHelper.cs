using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 规则匹配与分数计算。多条 matchItems 同时匹配则命中，按配置顺序优先，先匹配先返回。
    /// </summary>
    public static class ScoreRuleEvaluatorHelper
    {
        /// <summary>
        /// 采集当前设备 GPU/CPU 名称与主频。
        /// </summary>
        public static (string gpuName, string cpuName, int processorFrequencyMHz) GatherHardwareInfo()
        {
            string gpu = SystemInfo.graphicsDeviceName ?? "";
            string cpu = SystemInfo.processorType ?? "";
            int processorFreq = SystemInfo.processorFrequency;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (string.IsNullOrEmpty(cpu) || cpu.Contains("ARM") || cpu.Contains("aarch64"))
            {
                cpu = SystemInfo.deviceModel ?? cpu;
            }
#endif
            return (gpu, cpu, processorFreq);
        }

        /// <summary>
        /// 按规则顺序匹配，第一条所有 matchItems 均匹配的规则返回其 score；无匹配返回 0（解析时落到最高档）。
        /// </summary>
        public static int Evaluate(List<ScoreRuleDto> rules, string gpuName, string cpuName, int processorFrequencyMHz)
        {
            if (rules == null || rules.Count == 0)
            {
                return 0;
            }

            foreach (var rule in rules)
            {
                List<ScoreRuleMatchItemDto> items = rule.matchItems;
                if (items == null || items.Count == 0)
                {
                    continue;
                }

                bool allMatch = true;
                foreach (var item in items)
                {
                    if (!MatchItem(item, gpuName, cpuName, processorFrequencyMHz))
                    {
                        allMatch = false;
                        break;
                    }
                }

                if (allMatch)
                {
                    return rule.score;
                }
            }

            return 0;
        }

        private static bool MatchItem(ScoreRuleMatchItemDto item, string gpuName, string cpuName, int processorFrequencyMHz)
        {
            if (item.sourceType == (int)ScoreSourceTypeDto.CPUFrequency)
            {
                if (processorFrequencyMHz <= 0)
                {
                    return false;
                }

                return TryMatchProcessorFrequency(item.matchString, processorFrequencyMHz);
            }

            string source = item.sourceType == (int)ScoreSourceTypeDto.GPU ? gpuName : cpuName;
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            try
            {
                return Regex.IsMatch(source, item.matchString, RegexOptions.IgnoreCase);
            }
            catch (Exception e)
            {
                G.LogWarning($"[DeviceProfile] 正则匹配异常: {item.matchString}, {e.Message}");
                return false;
            }
        }

        // 解析 matchString 前缀（>=、<=、>、<）进行数值比较，无前缀时按 == 处理
        private static bool TryMatchProcessorFrequency(string matchString, int processorFrequencyMHz)
        {
            if (string.IsNullOrEmpty(matchString))
            {
                return false;
            }

            matchString = matchString.Trim();
            if (matchString.StartsWith(">="))
            {
                if (int.TryParse(matchString.Substring(2).Trim(), out int threshold))
                {
                    return processorFrequencyMHz >= threshold;
                }
            }
            else if (matchString.StartsWith("<="))
            {
                if (int.TryParse(matchString.Substring(2).Trim(), out int threshold))
                {
                    return processorFrequencyMHz <= threshold;
                }
            }
            else if (matchString.StartsWith(">"))
            {
                if (int.TryParse(matchString.Substring(1).Trim(), out int threshold))
                {
                    return processorFrequencyMHz > threshold;
                }
            }
            else if (matchString.StartsWith("<"))
            {
                if (int.TryParse(matchString.Substring(1).Trim(), out int threshold))
                {
                    return processorFrequencyMHz < threshold;
                }
            }
            else if (int.TryParse(matchString, out int threshold))
            {
                return processorFrequencyMHz == threshold;
            }

            return false;
        }
    }
}
