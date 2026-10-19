namespace Xease
{
    /// <summary>
    /// 本地化文本服务：按当前语言提供字符串表查询与格式化。
    /// 系统语言码、语言映射和 JSON 来源由宿主注入，接口不依赖 Unity。
    /// </summary>
    public interface ILocalizationService : IService
    {
        /// <summary>
        /// 当前语言。赋值时加载对应文本。
        /// </summary>
        Language Language { get; set; }

        /// <summary>
        /// 保存宿主给出的语言配置，并按已保存语言或系统语言码加载默认文本。
        /// </summary>
        /// <param name="languageCfg">语言映射与回落语言。配置或其 <see cref="ILocalizationLanguageCfg.SystemLanguageMap"/> 为空则不加载。</param>
        void Init(ILocalizationLanguageCfg languageCfg);

        /// <summary>
        /// 取本地化文本；没有则返回 key。
        /// </summary>
        string GetString(string key);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T>(string key, T param);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2>(string key, T1 param1, T2 param2);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2, T3>(string key, T1 param1, T2 param2, T3 param3);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2, T3, T4>(string key, T1 param1, T2 param2, T3 param3, T4 param4);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2, T3, T4, T5>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2, T3, T4, T5, T6>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2, T3, T4, T5, T6, T7>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString<T1, T2, T3, T4, T5, T6, T7, T8>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8);

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        string GetString(string key, params object[] param);

        /// <summary>
        /// 取未格式化的原文；没有则返回 null。
        /// </summary>
        string GetRawString(string key);

        /// <summary>
        /// 当前语言表是否包含该 key。
        /// </summary>
        bool HasRawString(string key);

        /// <summary>
        /// 按语言编号切换语言。编号非法时只打警告。
        /// </summary>
        void DebugChangeLanguage(byte languageCode);

        /// <summary>
        /// 打印全部语言编号。
        /// </summary>
        void DebugPrintAllLanguageCodes();
    }
}
