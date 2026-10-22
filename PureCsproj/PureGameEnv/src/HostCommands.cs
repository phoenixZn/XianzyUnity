using System;
using System.Collections.Generic;
using Xease;

namespace PureGameEnv
{
    /// <summary>
    /// 宿主字符串指令。help / quit / space 在这里消化；其余行交给逻辑（登入态接受任意非空行）。
    /// </summary>
    internal static class HostCommands
    {
        const string HelpText = "help  显示指令\nquit  退出\nlogin 登入\nspace 刷一个敌人（与空格键相同）";

        // 回车提交的行；每拍最多消化一行，避免空行把后续行一起判掉
        static readonly Queue<string> sPending = new Queue<string>();

        /// <summary>排入一行，由 PumpOne 每拍取出一行解析。</summary>
        public static void Submit(string line)
        {
            sPending.Enqueue(line ?? "");
        }

        /// <summary>解析队列中的下一行。</summary>
        public static void PumpOne()
        {
            if (sPending.Count == 0)
                return;
            Apply(sPending.Dequeue());
        }

        static void Apply(string line)
        {
            var cmd = (line ?? "").Trim();
            GameSession.NoteCommand(cmd);
            switch (cmd.ToLowerInvariant())
            {
                case "help":
                    ConsoleHostBridge.Write(HelpText);
                    return;
                case "quit":
                    GameSession.RequestQuit();
                    return;
                case "space":
                    ConsoleHostBridge.MarkKeyDown(ConsoleKey.Spacebar);
                    return;
                case "login":
                    ConsoleHostBridge.OfferCommand(cmd);
                    return;
                default:
                    ConsoleHostBridge.OfferCommand(cmd);
                    return;
            }
        }
    }
}
