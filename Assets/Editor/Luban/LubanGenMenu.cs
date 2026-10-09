using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// 调用 DataTables/gen_client.bat 生成 Luban 代码与 JSON。
/// </summary>
public static class LubanGenMenu
{
    /// <summary>
    /// 运行客户端 JSON 生成脚本，成功后刷新资源数据库。
    /// </summary>
    [MenuItem("HybridTool/生成 Luban 配置")]
    public static void GenerateClientJson()
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string bat = Path.Combine(projectRoot, "DataTables", "gen_client.bat");
        if (!File.Exists(bat))
        {
            Debug.LogError($"[Luban] missing gen script: {bat}");
            return;
        }

        var psi = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = "/c \"" + bat + "\"",
            WorkingDirectory = Path.GetDirectoryName(bat),
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            Debug.LogError("[Luban] failed to start gen_client.bat");
            return;
        }

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (!string.IsNullOrEmpty(stdout))
        {
            Debug.Log(stdout);
        }

        if (process.ExitCode != 0)
        {
            Debug.LogError($"[Luban] gen failed, exit={process.ExitCode}\n{stderr}");
            return;
        }

        if (!string.IsNullOrEmpty(stderr))
        {
            Debug.LogWarning(stderr);
        }

        AssetDatabase.Refresh();
        Debug.Log("[Luban] gen_client finished, AssetDatabase refreshed");
    }
}
