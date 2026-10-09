using cfg;

namespace Xease
{
    /// <summary>
    /// Luban 配表服务：持有生成的 <see cref="Tables"/>，数据来自 Asset RawFile 包。
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
        /// 按 Tables 所需文件名从 RawFile 包同步加载 JSON 并构造表。
        /// </summary>
        void Init();
    }
}
