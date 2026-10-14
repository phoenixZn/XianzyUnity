#if CONSOLE_CLIENT
using System;
using System.Collections.Generic;
#else
using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#endif

namespace Xease
{
    public static partial class G
    {
        /// <summary>
        /// 本地化文本服务。
        /// </summary>
        public static ILocalizationService Localization => GEnv.Inst.Services.LocalizationSvc;
        
        public static string GetI18N(string key, params string[] args)
        {
            var value = Localization.GetString(key);
            try
            {
                if (args != null && args.Length > 0)
                {
                    value = string.Format(value, args);
                }
            }
            catch (Exception e)
            {
                G.LogError($"GetLanguage key={key}, value={value}, args.Length={args?.Length ?? 0}: 格式化出现异常");
                throw;
            }
            if (string.IsNullOrEmpty(value))
            {
                G.LogError($"多语言配置key = {key} value == null, 请检查");
            }
            return value;
        }
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// 本地化文本：
        protected ILocalizationService _localizationSvc; // 本地化服务实例

        /// <summary>
        /// 已注册的本地化服务。
        /// </summary>
        public ILocalizationService LocalizationSvc => _localizationSvc;

#if CONSOLE_CLIENT
        // 与 UnityEngine.SystemLanguage 的整型值对齐，命令行宿主不引用该枚举
        private const int SystemLanguageChinese = 6;
        private const int SystemLanguageEnglish = 10;
        private const int SystemLanguageVietnamese = 38;
        private const int SystemLanguageChineseSimplified = 40;
        private const int SystemLanguageChineseTraditional = 41;
        private const int DefaultSystemLanguage = SystemLanguageEnglish;

        private static readonly Dictionary<int, Language> LanguageMap = new Dictionary<int, Language>
        {
            { SystemLanguageChinese, Language.zh_cn },
            { SystemLanguageChineseSimplified, Language.zh_cn },
            { SystemLanguageChineseTraditional, Language.zh_tw },
            { SystemLanguageEnglish, Language.en },
            { SystemLanguageVietnamese, Language.vn },
        };

        /// <summary>
        /// 注册本地化服务，并从仓库 JSON 目录加载默认语言。
        /// </summary>
        public void AddService_Localization()
        {
            G.Log("AddService_Localization");
            var svc = new LocalizationService(DefaultSystemLanguage, LanguageMap, LoadJsonFromFile);
            svc.Init();
            AddService(svc, out _localizationSvc);
        }

        // 向上查找 HotAssets 后读取语言名表与当前语言字符串
        private static void LoadJsonFromFile(Language language, Action<byte[], string> consume)
        {
            var assetsRoot = LocalizationDiskSource.FindAssetsRoot();
            if (assetsRoot == null)
            {
                G.LogError("[Localization] HotAssets config dir not found");
                return;
            }

            LocalizationDiskSource.Load(assetsRoot, language, consume);
        }
#else
        private const string AssetLanguageNameLocation = "language";

        private static readonly Dictionary<int, Language> LanguageMap = new Dictionary<int, Language>
        {
            { (int)SystemLanguage.Chinese, Language.zh_cn },
            { (int)SystemLanguage.ChineseSimplified, Language.zh_cn },
            { (int)SystemLanguage.ChineseTraditional, Language.zh_tw },
            { (int)SystemLanguage.English, Language.en },
            { (int)SystemLanguage.Vietnamese, Language.vn },
        };

        /// <summary>
        /// 注册本地化服务。播放态从 Asset RawFile 加载；编辑器非播放从 HotAssets 目录读取。
        /// </summary>
        public void AddService_Localization()
        {
            G.Log("AddService_Localization");
            var svc = new LocalizationService((int)Application.systemLanguage, LanguageMap, LoadJson);
            svc.Init();
            AddService(svc, out _localizationSvc);
        }

        // 编辑器非播放走磁盘；播放态走资源包
        private static void LoadJson(Language language, Action<byte[], string> consume)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                LocalizationDiskSource.Load(Application.dataPath, language, consume);
                return;
            }
#endif
            LoadJsonFromAsset(language, consume);
        }

        // location 为语言名表；tag 收集当前语言的字符串 JSON
        private static void LoadJsonFromAsset(Language language, Action<byte[], string> consume)
        {
            var assetService = G.Asset;
            var languageNameRawFile = assetService.LoadAssetRawFileSync(AssetLanguageNameLocation);
            if (languageNameRawFile != null)
            {
                consume(languageNameRawFile.GetRawFileData(), AssetLanguageNameLocation);
                assetService.Release(AssetLanguageNameLocation);
            }

            var package = assetService.RawFilePackage;
            //收集器里每种语言各写一份标签，例如简体中文：(目录: xxx/Localization/zh_cn/  标签: zh_cn;)
            var assetTag = language.ToString(); //约定的资源Tag
            var infos = package.GetAssetInfos(assetTag);
            if (infos == null || infos.Length == 0)
            {
                G.LogError($"AddService_Localization GetAssetInfos Empty! assetTag={assetTag}");
                return;
            }
            for (int i = 0; i < infos.Length; ++i)
            {
                var handle = package.LoadRawFileSync(infos[i]);
                if (handle == null)
                {
                    continue;
                }
                consume(handle.GetRawFileData(), infos[i].AssetPath);
                handle.Release();
            }
        }
#endif
    }
}
