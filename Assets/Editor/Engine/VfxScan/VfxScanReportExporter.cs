using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;

namespace Script.Editor.Extended.Engine.VfxScan
{
    public static class VfxScanReportExporter
    {
        public static bool ExportCsv(IReadOnlyList<VfxScanResult> results, string filePath, out string error)
        {
            error = string.Empty;
            if (string.IsNullOrEmpty(filePath))
            {
                error = "未选择导出路径。";
                return false;
            }

            var sb = new StringBuilder();
            sb.AppendLine("Status,Name,Path,Type,ParticleCount,DerivedParticles,MaterialCount,TextureCount,MaxTexture,IssueCount,Issues");

            if (results != null)
            {
                for (int i = 0; i < results.Count; i++)
                {
                    VfxScanResult result = results[i];
                    if (result == null)
                        continue;

                    sb.Append(Escape(StatusText(result.status))).Append(',');
                    sb.Append(Escape(result.PrefabName)).Append(',');
                    sb.Append(Escape(result.PrefabPath)).Append(',');
                    sb.Append(Escape(result.budgetDisplayName)).Append(',');
                    sb.Append(result.ParticleCount).Append(',');
                    sb.Append(result.TotalDerivedParticles).Append(',');
                    sb.Append(result.MaterialCount).Append(',');
                    sb.Append(result.TextureCount).Append(',');
                    sb.Append(Escape(FormatTexture(result))).Append(',');
                    sb.Append(result.issues != null ? result.issues.Count : 0).Append(',');
                    sb.Append(Escape(JoinIssues(result)));
                    sb.AppendLine();
                }
            }

            try
            {
                File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(true));
            }
            catch (System.Exception e)
            {
                error = e.Message;
                return false;
            }

            return true;
        }

        static string FormatTexture(VfxScanResult result)
        {
            if (result.metrics == null || (result.metrics.maxTextureWidth <= 0 && result.metrics.maxTextureHeight <= 0))
                return "无";
            return result.metrics.maxTextureWidth + "x" + result.metrics.maxTextureHeight;
        }

        static string JoinIssues(VfxScanResult result)
        {
            if (result.issues == null || result.issues.Count == 0)
                return string.Empty;

            var sb = new StringBuilder();
            for (int i = 0; i < result.issues.Count; i++)
            {
                if (i > 0)
                    sb.Append(" | ");
                sb.Append('[').Append(StatusText(result.issues[i].severity)).Append("] ");
                sb.Append(result.issues[i].title);
            }

            return sb.ToString();
        }

        static string StatusText(VfxHealthStatus status)
        {
            switch (status)
            {
                case VfxHealthStatus.Error: return "Error";
                case VfxHealthStatus.Warning: return "Warning";
                default: return "Pass";
            }
        }

        static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "\"\"";
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
    }
}
