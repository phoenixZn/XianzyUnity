using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 本地化语言。
    /// </summary>
    public enum Language : byte
    {
        unknown,
        /// <summary>
        /// 中文（简体）
        /// </summary>
        zh_cn,
        /// <summary>
        /// 中文（繁体）
        /// </summary>
        zh_tw,
        /// <summary>
        /// 英语
        /// </summary>
        en,
        /// <summary>
        /// 越南语
        /// </summary>
        vn,
    }
    
    public interface ILocalizationLanguageCfg
    {
        /// <summary>
        /// 系统语言码到 <see cref="Language"/> 的映射。
        /// </summary>
        public IReadOnlyDictionary<int, Language> SystemLanguageMap { get; }

        /// <summary>
        /// 没有已保存语言且系统语言码未命中时使用。
        /// </summary>
        public Language FallbackLanguage { get; }
    }
}
