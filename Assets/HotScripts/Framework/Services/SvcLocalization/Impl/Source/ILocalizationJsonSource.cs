namespace Xease
{
    /// <summary>
    /// Sink（汇）
    /// 接收一份本地化 JSON 原文；sourcePath 仅用于错误日志。
    /// </summary>
    internal interface ILocalizationJsonSink
    {
        /// <summary>
        /// 解析并写入一份 JSON。
        /// </summary>
        /// <param name="jsonBytes">UTF-8 JSON 原文。</param>
        /// <param name="sourcePath">来源路径或资源 location，便于定位解析失败。</param>
        void AddJson(byte[] jsonBytes, string sourcePath);
    }

    /// <summary>
    /// Source（源）
    /// 按语言读取本地化 JSON，逐份交给 sink。实现可以是磁盘或资源包。
    /// </summary>
    internal interface ILocalizationJsonSource
    {
        /// <summary>
        /// 加载语言名表与指定语言的字符串 JSON。
        /// </summary>
        void Load(Language language, ILocalizationJsonSink sink);
    }
}
