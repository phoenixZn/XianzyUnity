using cfg;

namespace Xease
{
    /// <summary>
    /// Luban 配表服务：持有生成的 <see cref="Tables"/>。表数据由宿主注入的 loader 提供，本接口不关心来源。
    /// </summary>
    public interface IConfigService : IService
    {
        /// <summary>
        /// 已加载的 Luban 表集合；未 Init 时为 null。
        /// </summary>
        Tables Tables { get; }

        /// <summary>
        /// 表是否已成功构造。
        /// </summary>
        bool Initialized { get; }

        /// <summary>
        /// 用宿主注入的 loader 同步构造表。
        /// </summary>
        void Init();
    }
}
