using System;
using IngameDebugConsole;

namespace Xease
{
    /// <summary>
    /// 功能块的命令行视图。由模块统一分发，块只处理自己的命令键。
    /// </summary>
    public interface IGmConsoleBlock
    {
        /// <summary>把本块命令登记进 <see cref="ModuleGM.CommandGroup"/>。</summary>
        void RegisterConsole();

        /// <summary>
        /// 处理一条命令。
        /// </summary>
        /// <param name="raw">控制台原文。</param>
        /// <returns>命令键属于本块时为 true，无论参数是否被接受。</returns>
        bool TryExecute(string raw);
    }


    //////////////////////////////////////////////////////////////////////////
    /// ModuleGM For DebugLogConsole:
    public partial class ModuleGM
    {
        public const string CommandGroup = "CmdGroup_GM"; // 各块注册命令时使用的 DebugLogConsole 组
        public const string ShowCanvasCommand = "gm.ui"; // 打开运行时调试窗，不属于具体功能块
        
        partial void BindLogConsole()
        {
            InitCustomDebugButtons();
            DebugLogConsole.SetSpecCommandHandler(CommandGroup, DispatchCommand);

            for (int i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i] is IGmConsoleBlock consoleBlock)
                    consoleBlock.RegisterConsole();
            }
        }

        partial void UnBindLogConsole()
        {
            DebugLogConsole.SetSpecCommandHandler(CommandGroup, IgnoreCommand);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:
        void InitCustomDebugButtons()
        {
            var debugLogMng = DebugLogManager.Instance;
            if (debugLogMng == null)
            {
                KLogger.LogWarning("InitCustomDebugButtons debugLogMng == null");
                return;
            }
            debugLogMng.clearCommandAfterExecution = false;
            debugLogMng.AddDebugButton("GM.UI", ShowCanvasCommand);
        }
        
        
        bool DispatchCommand(string raw)
        {
            if (DebugLogConsole.ExtractCmdKey(raw) == ShowCanvasCommand)
            {
                ShowCanvas();
                return true;
            }

            for (int i = 0; i < _blocks.Count; i++)
            {
                if (_blocks[i] is IGmConsoleBlock consoleBlock && consoleBlock.TryExecute(raw))
                    return true;
            }

            return false;
        }

        static bool IgnoreCommand(string raw)
        {
            return false;
        }

    }
}
