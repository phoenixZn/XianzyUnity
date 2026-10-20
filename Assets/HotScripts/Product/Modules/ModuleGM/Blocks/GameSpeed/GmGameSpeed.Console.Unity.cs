using System.Globalization;
using IngameDebugConsole;

namespace Xease
{
    public partial class GmGameSpeed : IGmConsoleBlock
    {
        //////////////////////////////////////////////////////////////////////////
        /// IGmConsoleBlock:

        /// <summary>登记本块命令。说明文本由 <see cref="FormatPresets"/> 生成。</summary>
        public void RegisterConsole()
        {
            DebugLogConsole.AddCommand_Spec(CommandName, ModuleGM.CommandGroup, "设置游戏基准倍速，可选 " + FormatPresets());
        }

        /// <summary>命令键匹配时打印当前倍速，或在命中预设后写入。</summary>
        public bool TryExecute(string raw)
        {
            if (DebugLogConsole.ExtractCmdKey(raw) != CommandName)
                return false;

            var arg = TakeArgument(raw);
            if (string.IsNullOrEmpty(arg))
            {
                G.Log("[GM] 当前基准倍速 " + FormatSpeed(Current) + "，可选 " + FormatPresets());
                return true;
            }

            if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var speed) || !TrySet(speed))
            {
                G.LogWarning("[GM] 拒绝倍速 " + arg + "，可选 " + FormatPresets());
                return true;
            }

            G.Log("[GM] 基准倍速 " + FormatSpeed(Current));
            return true;
        }

        // 兼容空格或逗号分隔，只取一个参数
        static string TakeArgument(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return string.Empty;

            raw = raw.Trim();
            var key = DebugLogConsole.ExtractCmdKey(raw);
            if (string.IsNullOrEmpty(key) || raw.Length <= key.Length)
                return string.Empty;

            var rest = raw.Substring(key.Length).Trim();
            if (rest.StartsWith(","))
                rest = rest.Substring(1).Trim();

            var split = rest.IndexOfAny(new[] { ' ', ',' });
            if (split >= 0)
                rest = rest.Substring(0, split).Trim();
            return rest;
        }
    }
}
