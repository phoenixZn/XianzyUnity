using System;
using System.Globalization;

namespace Xease
{
    /// <summary>
    /// 修改游戏基准倍速的 GM 功能块。预设只存在本类，供编辑器与真机窗做选项；写入不要求命中预设。
    /// 当前值读 <see cref="ITickTimeService.GameSpeed"/>。减速档也写基准倍速，不进入减速栈，也不改平台 timeScale。
    /// </summary>
    public partial class GmGameSpeed : IGmBlock
    {
        // 预设倍速选项
        public static readonly float[] Presets = {0.1f, 0.5f, 1f, 1.5f, 2f, 3f};

        // 与预设视为同一档的绝对差上限
        const float PresetMatchEpsilon = 0.001f;

        //////////////////////////////////////////////////////////////////////////
        /// IGmBlock:

        public string Id => "GmGameSpeed"; // IGmBlock插装标识

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
        /// 把倍速写入 <see cref="ITickTimeService.SetGameSpeed"/>。不按 <see cref="Presets"/> 过滤。
        /// </summary>
        /// <param name="speed">要写入的基准倍速。</param>
        /// <returns>已写入时为 true。</returns>
        public bool TrySet(float speed)
        {
            G.TickTime.SetGameSpeed(speed);
            OnSpeedApplied();
            return true;
        }

        /// <summary>倍速的展示文本</summary>
        public static string FormatSpeed(float speed)
        {
            return speed.ToString(CultureInfo.InvariantCulture);
        }

        // 视图侧刷新标签；无视图实现时调用会被抹掉
        partial void OnSpeedApplied();
    }
}
