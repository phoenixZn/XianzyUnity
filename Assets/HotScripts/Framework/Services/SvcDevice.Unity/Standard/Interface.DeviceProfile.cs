using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 硬件性能适配服务：按 CPU/GPU 规则评分选档，并应用渲染/特效开关。
    /// </summary>
    public interface IDeviceProfileService : IService
    {
        /// <summary>
        /// 当前生效挡位名；未初始化时为 null。
        /// </summary>
        string CurrentTierName { get; }

        /// <summary>
        /// 当前挡位数值索引；无法从挡位名解析时为 0。
        /// </summary>
        int CurrentTierIndex { get; }

        /// <summary>
        /// 当前挡位解析所用分数（覆盖档时仍为硬件评分）。
        /// </summary>
        int CurrentScore { get; }

        /// <summary>
        /// 硬件评分结果，与是否强制改档无关。
        /// </summary>
        int CurrentHardwareScore { get; }

        /// <summary>
        /// 硬件评分选档的档名；不受 <see cref="SetTierByName"/> 影响。
        /// </summary>
        string HardwareTierName { get; }

        /// <summary>
        /// 是否已完成一次成功的配置加载与选档。
        /// </summary>
        bool IsInitialized { get; }

        /// <summary>
        /// 当前挡位是否由 <see cref="SetTierByName"/> 强制覆盖。
        /// </summary>
        bool IsTierOverridden { get; }

        /// <summary>
        /// 按挡位名强制应用开关，不重新跑硬件评分；需已初始化。
        /// </summary>
        /// <param name="tierName">配置中的挡位名</param>
        /// <returns>应用成功为 true</returns>
        bool SetTierByName(string tierName);

        /// <summary>
        /// 恢复为硬件评分得到的挡位与开关。
        /// </summary>
        /// <returns>确实发生恢复为 true；未覆盖或未初始化为 false</returns>
        bool RestoreHardwareTier();

        /// <summary>
        /// 从挡位名解析数值索引；空或非法返回 0。
        /// </summary>
        int ResolveTierIndex(string tierName);

        /// <summary>
        /// 当前配置中可设置的档位名（保持 JSON 顺序）；未初始化时为空列表。
        /// </summary>
        IReadOnlyList<string> GetAvailableTiers();

        /// <summary>
        /// 指定档位的推荐帧率；未找到或未初始化返回 0。
        /// </summary>
        int GetRecommendedFps(string tierName);

        /// <summary>
        /// 输出当前挡位与硬件信息。
        /// </summary>
        void LogDeviceProfileInfo();

        /// <summary>
        /// 按挡位名强制覆盖当前档（需已初始化）。
        /// </summary>
        void ConsoleSetTier(string tierName);
    }
}
