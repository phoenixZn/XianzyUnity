#if !CONSOLE_CLIENT
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Xease
{
    /// <summary>
    /// Unity 宿主的 JSON 来源：RawFile。
    /// </summary>
    internal sealed class LocalizationAssetSource : ILocalizationJsonSource
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 语言名表 RawFile location
        private const string AssetLanguageNameLocation = "language";

        // location 为语言名表；tag 收集当前语言的字符串 JSON
        private static void LoadFromAsset(Language language, ILocalizationJsonSink sink)
        {
            var assetService = G.Asset;
            var languageNameRawFile = assetService.LoadAssetRawFileSync(AssetLanguageNameLocation);
            if (languageNameRawFile != null)
            {
                //语言名表: 这个.json存的是各语言的“自称”
                sink.AddJson(languageNameRawFile.GetRawFileData(), AssetLanguageNameLocation);
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

                sink.AddJson(handle.GetRawFileData(), infos[i].AssetPath);
                handle.Release();
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// ILocalizationJsonSource:

        /// <summary>
        /// 编辑器非播放走磁盘；播放态与真机走资源包。
        /// </summary>
        public void Load(Language language, ILocalizationJsonSink sink)
        {
#if UNITY_EDITOR
            //编辑器非播放转磁盘
            if (!EditorApplication.isPlaying)
            {
                var disk = new LocalizationDiskSource(Application.dataPath);
                disk.Load(language, sink);
                return;
            }
#endif
            LoadFromAsset(language, sink);
        }
    }
}
#endif
