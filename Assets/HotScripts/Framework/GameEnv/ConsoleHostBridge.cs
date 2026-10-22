#if CONSOLE_CLIENT
using System;
using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 命令行宿主与逻辑之间的日志、行指令、即时键接缝。
    /// 由 PureGameEnv 在 GameEntryInit 之前安装；未安装时日志仍写到控制台。
    /// </summary>
    public static class ConsoleHostBridge
    {
        // 宿主安装的日志出口；TUI 收进日志区，CLI 写 stdout
        static Action<string> sWrite;
        // 本帧尚未被逻辑取走的行指令
        static readonly Queue<string> sCommands = new Queue<string>();
        // 本帧新投入、且尚未取走的行数；帧末仍剩余则视为无人识别
        static int sOfferedThisFrame;
        // 本帧按下的键；帧末清除
        static readonly HashSet<ConsoleKey> sKeys = new HashSet<ConsoleKey>();

        /// <summary>
        /// 启动时由宿主写入。为 true 时登入态按管道处理，不读键盘。
        /// </summary>
        public static bool IsInputRedirected { get; set; }

        /// <summary>
        /// 安装日志出口。传 null 则恢复为直接写控制台。
        /// </summary>
        public static void BindWrite(Action<string> write)
        {
            sWrite = write;
        }

        /// <summary>
        /// 写一行日志。宿主未安装时写到标准输出。
        /// </summary>
        public static void Write(string message)
        {
            if (message == null)
                return;
            if (sWrite != null)
                sWrite(message);
            else
                Console.WriteLine(message);
        }

        /// <summary>
        /// 投入一行，供本帧及之后的逻辑取走。
        /// </summary>
        public static void OfferCommand(string line)
        {
            sCommands.Enqueue(line ?? "");
            sOfferedThisFrame++;
        }

        /// <summary>
        /// 取走队列中的下一行。没有则返回 false。
        /// </summary>
        public static bool TryTakeCommand(out string line)
        {
            if (sCommands.Count == 0)
            {
                line = null;
                return false;
            }

            line = sCommands.Dequeue();
            if (sOfferedThisFrame > 0)
                sOfferedThisFrame--;
            return true;
        }

        /// <summary>
        /// 标记本帧某键已按下。同一键一帧只保留一次。
        /// </summary>
        public static void MarkKeyDown(ConsoleKey key)
        {
            sKeys.Add(key);
        }

        /// <summary>
        /// 本帧是否按下过该键。
        /// </summary>
        public static bool WasKeyDown(ConsoleKey key)
        {
            return sKeys.Contains(key);
        }

        /// <summary>
        /// 帧末调用：没人取走的非空行记为未知指令，并清除本帧按键。
        /// </summary>
        public static void EndFrame()
        {
            while (sOfferedThisFrame > 0 && sCommands.Count > 0)
            {
                var line = sCommands.Dequeue();
                sOfferedThisFrame--;
                if (!string.IsNullOrWhiteSpace(line))
                    Write("未知指令");
            }

            sOfferedThisFrame = 0;
            sKeys.Clear();
        }
    }
}
#endif
