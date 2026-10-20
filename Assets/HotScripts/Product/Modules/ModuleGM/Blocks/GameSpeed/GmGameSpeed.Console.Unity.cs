using System;
using System.Globalization;
using IngameDebugConsole;

namespace Xease
{
    public partial class GmGameSpeed : IGmConsoleBlock
    {
        const float ConsoleSpeedMin = 0f; // 命令行允许写入的倍速下界（含）
        const float ConsoleSpeedMax = 100f; // 命令行允许写入的倍速上界（含）

        //////////////////////////////////////////////////////////////////////////
        /// IGmConsoleBlock:

        /// <summary>登记本块命令。说明文本带安全区间，并附上 <see cref="FormatPresets"/>。</summary>
        public void RegisterConsole()
        {
            DebugLogConsole.AddCommand_Spec(CommandName, ModuleGM.CommandGroup, "设置游戏基准倍速，" + FormatConsoleHint());
        }

        /// <summary>命令键匹配时打印当前倍速；参数落在安全区间内则写入，接近预设时吸附到该档。</summary>
        public bool TryExecute(string raw)
        {
            if (DebugLogConsole.ExtractCmdKey(raw) != CommandName)
                return false;

            var arg = TakeArgument(raw);
            if (string.IsNullOrEmpty(arg))
            {
                G.Log("[GM] 当前基准倍速 " + FormatSpeed(Current) + "，" + FormatConsoleHint());
                return true;
            }

            if (!float.TryParse(arg, NumberStyles.Float, CultureInfo.InvariantCulture, out var speed)
                || float.IsNaN(speed) || float.IsInfinity(speed)
                || speed < ConsoleSpeedMin || speed > ConsoleSpeedMax)
            {
                G.LogWarning("[GM] 拒绝倍速 " + arg + "，" + FormatConsoleHint());
                return true;
            }

            if (TryMatchPreset(speed, out var preset))
                speed = preset;

            TrySet(speed);
            G.Log("[GM] 基准倍速 " + FormatSpeed(Current));
            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 命令说明、查询和拒绝日志共用的区间与预设提示
        static string FormatConsoleHint()
        {
            return "允许 " + FormatSpeed(ConsoleSpeedMin) + "～" + FormatSpeed(ConsoleSpeedMax) + "，预设 " + FormatPresets();
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

        // 解析值接近某档预设时吸附到该档；只给控制台文本入口用
        bool TryMatchPreset(float speed, out float preset)
        {
            for (int i = 0; i < Presets.Length; i++)
            {
                if (Math.Abs(Presets[i] - speed) <= PresetMatchEpsilon)
                {
                    preset = Presets[i];
                    return true;
                }
            }

            preset = 0f;
            return false;
        }
    }
}
