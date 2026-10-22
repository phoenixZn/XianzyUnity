using System;
using Xease;
using Xease.CoreGame;

namespace PureGameEnv
{
    /// <summary>
    /// CLI 与 TUI 共用的一拍逻辑：消化一行指令、转发空格、泵 GameEntry。
    /// </summary>
    internal static class GameSession
    {
        /// <summary>最近一次提交的指令原文，供战斗信息区显示。</summary>
        public static string LastCommand { get; private set; } = "";

        /// <summary>宿主已请求退出主循环。</summary>
        public static bool QuitRequested { get; private set; }

        /// <summary>请求主循环在本拍结束后退出。</summary>
        public static void RequestQuit()
        {
            QuitRequested = true;
        }

        /// <summary>记下最近指令，供战斗信息区显示。</summary>
        public static void NoteCommand(string line)
        {
            LastCommand = line ?? "";
        }

        /// <summary>
        /// 一拍。调用前本帧按键应已标记。
        /// </summary>
        public static void Tick(GameEntry entry)
        {
            HostCommands.PumpOne();

            if (ConsoleHostBridge.WasKeyDown(ConsoleKey.Spacebar) && GEnv.Inst != null)
                G.ValueEvent.Dispatch(new EvtDemoSpacePressed());

            entry.FixedUpdate();
            entry.Update();
            entry.LateUpdate();
            ConsoleHostBridge.EndFrame();
        }
    }
}
