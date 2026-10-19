using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 产品侧的系统语言映射：把宿主系统语言码对应到 <see cref="Language"/>，并给出未命中时的回落语言。
    /// 由游戏环境在本地化服务 Init 时传入；增减语言支持只改本类。
    /// </summary>
    public sealed class LocalizationLanguageTable : ILocalizationLanguageCfg
    {
        // 与 UnityEngine.SystemLanguage 整型值对齐
        private const int SystemLanguageChinese = 6;
        private const int SystemLanguageEnglish = 10;
        private const int SystemLanguageVietnamese = 38;
        private const int SystemLanguageChineseSimplified = 40;
        private const int SystemLanguageChineseTraditional = 41;
        
        /// <summary>
        /// 填入当前产品支持的系统语言映射与回落语言。
        /// </summary>
        public LocalizationLanguageTable()
        {
            SystemLanguageMap = new Dictionary<int, Language>
            {
                { SystemLanguageChinese, Language.zh_cn },
                { SystemLanguageChineseSimplified, Language.zh_cn },
                { SystemLanguageChineseTraditional, Language.zh_tw },
                { SystemLanguageEnglish, Language.en },
                { SystemLanguageVietnamese, Language.vn },
            };
            FallbackLanguage = Language.en;
        }
        
        //////////////////////////////////////////////////////////////////////////
        /// ILocalizationLanguageCfg:

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
