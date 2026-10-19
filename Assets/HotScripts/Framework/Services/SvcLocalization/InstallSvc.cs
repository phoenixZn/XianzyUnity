using System;
#if !CONSOLE_CLIENT
using UnityEngine;
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

        /// <summary>
        /// 注册本地化服务，并用 <paramref name="languageCfg"/> 加载默认语言。
        /// </summary>
        public ILocalizationService AddService_Localization(ILocalizationLanguageCfg languageCfg)
        {
            G.Log("AddService_Localization");
#if CONSOLE_CLIENT
            int? systemLanguage = null;
            ILocalizationJsonSource source = LocalizationDiskSource.FromAppBase();
#else
            int? systemLanguage = (int)Application.systemLanguage;
            ILocalizationJsonSource source = new LocalizationAssetSource();
#endif
            var svc = new LocalizationService(systemLanguage, source);
            AddService(svc, out _localizationSvc);
            svc.Init(languageCfg);
            return svc;
        }
    }
}
