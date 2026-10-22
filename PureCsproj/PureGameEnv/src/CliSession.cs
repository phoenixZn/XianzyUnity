using System;
using System.IO;
using System.Text;
using System.Threading;
using Xease;

namespace PureGameEnv
{
    /// <summary>
    /// 流式宿主。日志写 stdout；交互终端下自行组行，不阻塞帧泵。
    /// </summary>
    internal sealed class CliSession
    {
        // 与改造前主循环相同的让出间隔
        const int FrameWaitMs = 20;

        // 尚未回车的半行
        readonly StringBuilder _line = new StringBuilder();

        /// <summary>在 GameEntryInit 之前接上标准输出日志。</summary>
        public void Install()
        {
            ConsoleHostBridge.IsInputRedirected = IsInputRedirected();
            ConsoleHostBridge.BindWrite(Console.WriteLine);
            ConsoleHostBridge.Write("PureGameEnv cli");
        }

        /// <summary>泵帧直到 quit 或 Ctrl+C。</summary>
        public void Run(GameEntry entry)
        {
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                GameSession.RequestQuit();
            };

            while (!GameSession.QuitRequested)
            {
                PollKeys();
                GameSession.Tick(entry);
                if (GameSession.QuitRequested)
                    break;
                Thread.Sleep(FrameWaitMs);
            }
        }

        // 管道不读键盘，登入态会自行进入主状态
        void PollKeys()
        {
            if (ConsoleHostBridge.IsInputRedirected)
                return;

            try
            {
                while (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(intercept: true);
                    if (key.Key == ConsoleKey.Spacebar)
                    {
                        ConsoleHostBridge.MarkKeyDown(ConsoleKey.Spacebar);
                        GameSession.NoteCommand("space");
                        continue;
                    }

                    if (key.Key == ConsoleKey.Enter)
                    {
                        Console.WriteLine();
                        HostCommands.Submit(_line.ToString());
                        _line.Clear();
                        continue;
                    }

                    if (key.Key == ConsoleKey.Backspace)
                    {
                        if (_line.Length == 0)
                            continue;
                        _line.Length -= 1;
                        Console.Write("\b \b");
                        continue;
                    }

                    if (char.IsControl(key.KeyChar))
                        continue;

                    _line.Append(key.KeyChar);
                    Console.Write(key.KeyChar);
                }
            }
            catch (InvalidOperationException)
            {
                ConsoleHostBridge.IsInputRedirected = true;
            }
            catch (IOException)
            {
                ConsoleHostBridge.IsInputRedirected = true;
            }
        }

        static bool IsInputRedirected()
        {
            try
            {
                return Console.IsInputRedirected;
            }
            catch (IOException)
            {
                return true;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
        }
    }
}
