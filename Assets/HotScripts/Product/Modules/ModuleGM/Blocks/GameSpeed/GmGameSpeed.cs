using System;
using System.Globalization;
using System.Text;

namespace Xease
{
    /// <summary>
    /// 修改游戏基准倍速的 GM 功能块。预设只存在本类；编辑器、命令行、真机窗都调用这里。
    /// 当前值读 <see cref="ITickTimeService.GameSpeed"/>。减速档也写基准倍速，不进入减速栈，也不改平台 timeScale。
    /// </summary>
    public partial class GmGameSpeed : IGmBlock
    {
        public const string BlockId = "game_speed"; // 插装标识
        public const string CommandName = "speed"; // 本块独占的命令键

        // 允许写入的基准倍速。三处视图只读这一份，不另存列表
        public static readonly float[] Presets = { 0.5f, 1f, 1.5f, 2f };

        // 文本解析值与预设视为同一档的绝对差上限
        const float PresetMatchEpsilon = 0.001f;

        //////////////////////////////////////////////////////////////////////////
        /// IGmBlock:

        public string Id => BlockId;

        public string Title => "修改游戏速度";

        /// <summary>本块所在的顶条页。编辑器和真机窗共用。</summary>
        public string Tab => "Base";

        //////////////////////////////////////////////////////////////////////////
        /// This：

        /// <summary>当前基准倍速，读时间服务，不在块内缓存。</summary>
        public float Current => G.TickTime.GameSpeed;

        /// <summary>该数值是否与当前基准倍速为同一档。</summary>
        public bool IsCurrent(float speed)
        {
            return Math.Abs(Current - speed) <= PresetMatchEpsilon;
        }

        /// <summary>
        /// 命中 <see cref="Presets"/> 时，把该档写入 <see cref="ITickTimeService.SetGameSpeed"/>。
        /// </summary>
        /// <param name="speed">候选倍速，通常来自预设或命令参数。</param>
        /// <returns>命中预设并已写入时为 true。</returns>
        public bool TrySet(float speed)
        {
            if (!TryMatchPreset(speed, out var preset))
                return false;

            G.TickTime.SetGameSpeed(preset);
            OnSpeedApplied();
            return true;
        }

        /// <summary>把 <see cref="Presets"/> 拼成展示文本。</summary>
        public static string FormatPresets()
        {
            var text = new StringBuilder();
            for (int i = 0; i < Presets.Length; i++)
            {
                if (i > 0)
                    text.Append('/');
                text.Append(FormatSpeed(Presets[i]));
            }

            return text.ToString();
        }

        /// <summary>倍速的展示文本，三处视图共用。</summary>
        public static string FormatSpeed(float speed)
        {
            return speed.ToString(CultureInfo.InvariantCulture);
        }

        // 视图侧刷新标签；无视图实现时调用会被抹掉
        partial void OnSpeedApplied();

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
