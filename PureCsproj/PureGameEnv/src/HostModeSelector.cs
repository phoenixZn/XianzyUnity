using System;
using System.IO;

namespace PureGameEnv
{
    /// <summary>
    /// 同一 exe 的两种宿主。CLI 保持 stdout 流；TUI 接管屏幕。
    /// </summary>
    internal enum HostMode
    {
        Cli,
        Tui
    }

    /// <summary>
    /// 按参数和终端是否被重定向选择宿主。--cli / --tui 优先于自动判断。
    /// </summary>
    internal static class HostModeSelector
    {
        /// <summary>
        /// 解析启动模式。失败时 error 可直接写到 stderr，调用方不要初始化 TUI。
        /// </summary>
        public static bool TryResolve(string[] args, out HostMode mode, out string error)
        {
            var cli = false;
            var tui = false;
            if (args != null)
            {
                for (var i = 0; i < args.Length; i++)
                {
                    if (IsFlag(args[i], "--cli"))
                        cli = true;
                    else if (IsFlag(args[i], "--tui"))
                        tui = true;
                }
            }

            if (cli && tui)
            {
                mode = HostMode.Cli;
                error = "不能同时指定 --cli 与 --tui。";
                return false;
            }

            if (tui)
            {
                mode = HostMode.Tui;
                if (!HasInteractiveConsole())
                {
                    error = "TUI 需要交互终端。批处理请使用 --cli。";
                    return false;
                }

                error = null;
                return true;
            }

            if (cli)
            {
                mode = HostMode.Cli;
                error = null;
                return true;
            }

            mode = HasInteractiveConsole() ? HostMode.Tui : HostMode.Cli;
            error = null;
            return true;
        }

        // 输入或输出被管道接走时不能画 TUI
        static bool HasInteractiveConsole()
        {
            try
            {
                return !Console.IsInputRedirected && !Console.IsOutputRedirected;
            }
            catch (IOException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        static bool IsFlag(string arg, string name)
        {
            return string.Equals(arg, name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
