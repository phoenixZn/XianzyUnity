using UnityEngine;
using Xease.Engine;
using Xease.Engine.Core;

namespace Xease
{
    /// <summary>
    /// 将解析后的挡位开关应用到 Unity 渲染/质量设置。
    /// </summary>
    public class DeviceProfileApplier
    {
        // 与 KaboomLit.shader 中 multi_compile_global 对应，低档 Shader Tier 时启用
        public const string QualityLowShaderKeyword = "QUALITY_LOW";

        private const string OutlineCVarName = "r.OutlineEnable";
        private const string ShadowCVarName = "r.ShadowmapEnable";
        private const string PostProcessCVarName = "r.PostProcessingEnable";

        // 最近一次成功 Apply 的开关快照；Cleanup 时清空
        private ResolvedTierSwitches _lastAppliedSwitches;

        /// <summary>
        /// 应用所有开关（QualitySettings、CVar、特效 LOD）。相机相关开关需主相机存在时再应用。
        /// </summary>
        public void Apply(ResolvedTierSwitches switches)
        {
            if (switches == null)
            {
                return;
            }

            _lastAppliedSwitches = switches;

            // Shader Tier 先于其它覆盖：SetQualityLevel 可能重置质量项
            int tier = Mathf.Clamp(switches.ShaderTier, 0, 2);
            int qualityLevel = tier switch
            {
                0 => 0,
                1 => 1,
                _ => Mathf.Min(2, QualitySettings.names.Length - 1)
            };
            QualitySettings.SetQualityLevel(qualityLevel, true);

            ApplyQualityLowGlobalKeyword(switches);

            ApplyBoolCVar(OutlineCVarName, switches.OutlineEnabled);
            ApplyBoolCVar(ShadowCVarName, switches.ShadowEnabled);
            ApplyBoolCVar(PostProcessCVarName, switches.PostProcessEnabled);

            SetVisualEffectLOD(switches.VisualEffectLOD);
        }

        /// <summary>
        /// 清除本服务写入的 Keyword / CVar（用于退出时）。
        /// </summary>
        public void Cleanup()
        {
            Shader.DisableKeyword(QualityLowShaderKeyword);

            ResetBoolCVars(OutlineCVarName);
            ResetBoolCVars(ShadowCVarName);
            ResetBoolCVars(PostProcessCVarName);

            _lastAppliedSwitches = null;
        }

        private void SetVisualEffectLOD(int lod)
        {
            if (VisualEffectLOD.GlobalLevel == lod)
            {
                G.Log($"[DeviceProfile] 特效LOD 已设置为 {lod} ，无需重复设置");
                return;
            }

            VisualEffectLOD.SetGlobalLevel(lod);
        }

        private static void ApplyBoolCVar(string cVarName, bool value)
        {
            CVarBool cVar = ConsoleManager.FindCVar(cVarName) as CVarBool;
            cVar?.SetValue(value, ConsolePriority.ConfigFile);
        }

        private static void ResetBoolCVars(string cVarName)
        {
            CVarBool cVar = ConsoleManager.FindCVar(cVarName) as CVarBool;
            cVar?.SetValue(cVar.DefaultValue, ConsolePriority.ConfigFile);
        }

        private static void ApplyQualityLowGlobalKeyword(ResolvedTierSwitches switches)
        {
            if (switches == null)
            {
                return;
            }

            if (switches.ShaderTier == 0)
            {
                Shader.EnableKeyword(QualityLowShaderKeyword);
                return;
            }

            Shader.DisableKeyword(QualityLowShaderKeyword);
        }
    }
}
