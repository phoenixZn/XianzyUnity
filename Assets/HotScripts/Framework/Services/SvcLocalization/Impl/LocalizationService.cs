using System;
using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 本地化文本服务：按语言加载 JSON 字符串表，并提供格式化取值。
    /// 系统语言码与 JSON 来源由宿主注入，本类不引用 Unity。
    /// </summary>
    internal class LocalizationService : ILocalizationService
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 与 UnityEngine.SystemLanguage 整型值对齐，本类不引用该枚举
        private const int SystemLanguageChinese = 6;
        private const int SystemLanguageEnglish = 10;
        private const int SystemLanguageVietnamese = 38;
        private const int SystemLanguageChineseSimplified = 40;
        private const int SystemLanguageChineseTraditional = 41;

        // 命令行宿主无系统语言时注入的语言码
        internal const int ConsoleDefaultSystemLanguage = SystemLanguageEnglish;

        // 系统语言码到 Language 的映射
        private static readonly Dictionary<int, Language> LanguageMap = new Dictionary<int, Language>
        {
            { SystemLanguageChinese, Language.zh_cn },
            { SystemLanguageChineseSimplified, Language.zh_cn },
            { SystemLanguageChineseTraditional, Language.zh_tw },
            { SystemLanguageEnglish, Language.en },
            { SystemLanguageVietnamese, Language.vn },
        };

        // 宿主给出的系统语言码，仅在没有已保存语言时参与映射
        private readonly int _systemLanguageCode;
        // 宿主注入的 JSON 来源：磁盘或资源包
        private readonly ILocalizationJsonSource _jsonSource;
        // 当前语言
        private Language _language = Language.unknown;
        // 当前语言的字符串表
        private readonly LocalizationData _localizationData = new();

        /// <summary>
        /// 保存宿主注入的系统语言码与 JSON 来源。
        /// </summary>
        public LocalizationService(int systemLanguageCode, ILocalizationJsonSource jsonSource)
        {
            _systemLanguageCode = systemLanguageCode;
            _jsonSource = jsonSource ?? throw new ArgumentNullException(nameof(jsonSource));
        }

        // 解析默认语言并强制重载字符串表
        private void LoadDefaultLanguage()
        {
            var language = GetDefaultLanguage();
            _language = language;
            _localizationData.Load(language, _jsonSource, true);
            G.Log($"[Localization] Init as language {language}");
        }

        // 已保存语言优先，否则用系统语言码映射，仍未命中则回落英语
        private Language GetDefaultLanguage()
        {
            var setLanguage = LoadLanguageSettings();
            if (setLanguage != Language.unknown)
            {
                return setLanguage;
            }

            if (LanguageMap.TryGetValue(_systemLanguageCode, out var language))
            {
                return language;
            }

            return Language.en;
        }

        // 切换语言并加载文本；相同语言不重复加载
        private void SetLanguage(Language language)
        {
            if (_language == language)
            {
                return;
            }

            _language = language;
            _localizationData.Load(language, _jsonSource);
            SaveLanguageSettings();
            G.Log($"[Localization] Set language {language}");
        }

        /// <summary>
        /// 持久化当前语言。尚未接入存储。
        /// </summary>
        public void SaveLanguageSettings()
        {
        }

        /// <summary>
        /// 读取已保存语言。尚未接入存储时返回 <see cref="Language.unknown"/>。
        /// </summary>
        public Language LoadLanguageSettings()
        {
            return Language.unknown;
        }

        // 缺 key 时按需打日志并返回 key 本身
        private string Resolve(string key, bool logMissing, Func<string, string> format)
        {
            var value = GetRawString(key);
            if (value == null)
            {
                if (logMissing)
                {
                    G.LogError($"[Localization] GetString Failed. key: {key}");
                }
                return key;
            }

            return format(value);
        }

        //////////////////////////////////////////////////////////////////////////
        /// ILocalizationService:

        /// <summary>
        /// 当前语言。赋值时加载对应文本。
        /// </summary>
        public Language Language
        {
            get => _language;
            set => SetLanguage(value);
        }

        /// <summary>
        /// 按已保存语言或系统语言码加载默认文本。
        /// </summary>
        public void Init()
        {
            LoadDefaultLanguage();
        }

        /// <summary>
        /// 取本地化文本；没有则返回 key。
        /// </summary>
        public string GetString(string key)
        {
            return Resolve(key, false, value => value);
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T>(string key, T param)
        {
            return Resolve(key, false, value => LnString.Format(value, param));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2>(string key, T1 param1, T2 param2)
        {
            return Resolve(key, false, value => LnString.Format(value, param1, param2));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2, T3>(string key, T1 param1, T2 param2, T3 param3)
        {
            return Resolve(key, true, value => LnString.Format(value, param1, param2, param3));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2, T3, T4>(string key, T1 param1, T2 param2, T3 param3, T4 param4)
        {
            return Resolve(key, true, value => LnString.Format(value, param1, param2, param3, param4));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2, T3, T4, T5>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5)
        {
            return Resolve(key, true, value => LnString.Format(value, param1, param2, param3, param4, param5));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2, T3, T4, T5, T6>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6)
        {
            return Resolve(key, true, value => LnString.Format(value, param1, param2, param3, param4, param5, param6));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2, T3, T4, T5, T6, T7>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7)
        {
            return Resolve(key, true, value => LnString.Format(value, param1, param2, param3, param4, param5, param6, param7));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString<T1, T2, T3, T4, T5, T6, T7, T8>(string key, T1 param1, T2 param2, T3 param3, T4 param4, T5 param5, T6 param6, T7 param7, T8 param8)
        {
            return Resolve(key, true, value => LnString.Format(value, param1, param2, param3, param4, param5, param6, param7, param8));
        }

        /// <summary>
        /// 取本地化文本并填入参数；没有则返回 key。
        /// </summary>
        public string GetString(string key, params object[] param)
        {
            return Resolve(key, true, value => LnString.Format(value, param));
        }

        /// <summary>
        /// 取未格式化的原文；没有则返回 null。
        /// </summary>
        public string GetRawString(string key)
        {
            return _localizationData.GetRawString(key);
        }

        /// <summary>
        /// 当前语言表是否包含该 key。
        /// </summary>
        public bool HasRawString(string key) => _localizationData.HasRawString(key);

        /// <summary>
        /// 按语言编号切换语言。编号非法时只打警告。
        /// </summary>
        public void DebugChangeLanguage(byte languageCode)
        {
            if (!Enum.IsDefined(typeof(Language), languageCode))
            {
                G.LogWarning($"[Localization] {languageCode} is not a valid language");
                return;
            }

            var language = (Language)languageCode;
            if (language == Language.unknown)
            {
                G.LogWarning($"[Localization] {languageCode} is not a valid language");
                return;
            }
            Language = language;
        }

        /// <summary>
        /// 打印全部语言编号。
        /// </summary>
        public void DebugPrintAllLanguageCodes()
        {
            var str = "[Localization] Language codes:";
            foreach (Language language in Enum.GetValues(typeof(Language)))
            {
                if (language == Language.unknown)
                    continue;
                str += $"\n{language.DisplayName()}({language}): {(byte)language}";
            }
            G.LogWarning(str);
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService:

        /// <summary>
        /// 丢弃当前语言与字符串表。
        /// </summary>
        public void Shutdown()
        {
            _language = Language.unknown;
            _localizationData.Unload();
        }
    }
}
