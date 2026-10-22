using System;
using System.IO;
using System.Text;
using Xease;

namespace PureGameEnv
{
    internal static class Program
    {
        static int Main(string[] args)
        {
            // 日志按 UTF-8 写出，便于 Windows Terminal 与管道读取
            TryUseUtf8Stdout();

            if (!HostModeSelector.TryResolve(args, out var mode, out var error))
            {
                Console.Error.WriteLine(error);
                return 1;
            }

            GameEntry entry = null;
            //mode = HostMode.Cli;  //Force set debug: 
            try
            {
                if (mode == HostMode.Cli)
                {
                    var cli = new CliSession();
                    cli.Install();
                    entry = GameEntry.GameEntryInit();
                    cli.Run(entry);
                }
                else
                {
                    var tui = new TuiSession();
                    tui.Install();
                    entry = GameEntry.GameEntryInit();
                    tui.Run(entry);
                }
            }
            finally
            {
                entry?.DestroyEntry();
            }

            return 0;
        }

        static void TryUseUtf8Stdout()
        {
            try
            {
                Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            }
            catch (IOException)
            {
            }
            catch (InvalidOperationException)
            {
            }
        }
    }
}
