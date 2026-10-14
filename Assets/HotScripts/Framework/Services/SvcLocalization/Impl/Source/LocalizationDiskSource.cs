using System;
using System.IO;

namespace Xease
{
    /// <summary>
    /// 从仓库 HotAssets 目录读取本地化 JSON。Unity 编辑器非播放与命令行宿主共用，不依赖资源包。
    /// </summary>
    internal sealed class LocalizationDiskSource : ILocalizationJsonSource
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 仓库 Assets 根；FindAssetsRoot 成功后复用
        private static string _cachedAssetsRoot;
        // 已知 Assets 根；FromAppBase 时为空，改由 FindAssetsRoot 解析
        private readonly string _assetsRoot;
        // 为 true 时每次 Load 从程序输出目录向上查找仓库根
        private readonly bool _resolveFromAppBase;

        /// <summary>
        /// 使用调用方给出的 Assets 根目录。
        /// </summary>
        public LocalizationDiskSource(string assetsRoot)
        {
            _assetsRoot = assetsRoot;
            _resolveFromAppBase = false;
        }

        private LocalizationDiskSource()
        {
            _resolveFromAppBase = true;
        }

        /// <summary>
        /// 命令行宿主：Load 时从程序输出目录向上解析仓库 Assets 根。
        /// </summary>
        public static LocalizationDiskSource FromAppBase()
        {
            return new LocalizationDiskSource();
        }

        /// <summary>
        /// 从程序输出目录向上查找仓库 Assets 根目录。找不到返回 null。
        /// </summary>
        public static string FindAssetsRoot()
        {
            if (_cachedAssetsRoot != null)
            {
                return _cachedAssetsRoot;
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "HotAssets", "Config");
                if (Directory.Exists(candidate))
                {
                    _cachedAssetsRoot = Path.Combine(dir.FullName, "Assets");
                    return _cachedAssetsRoot;
                }

                dir = dir.Parent;
            }

            return null;
        }

        // 读语言名表与 str_main 下全部 JSON，逐份交给 sink
        private static void LoadFiles(string assetsRoot, Language language, ILocalizationJsonSink sink)
        {
            if (string.IsNullOrEmpty(assetsRoot))
            {
                G.LogError($"Load assetsRoot IsNullOrEmpty");
                return;
            }

            var languageNamePath = Path.Combine(assetsRoot, "HotAssets", "Config", "App", "language.json");
            if (File.Exists(languageNamePath))
            {
                sink.AddJson(File.ReadAllBytes(languageNamePath), languageNamePath);
            }

            var stringsDir = Path.Combine(assetsRoot, "HotAssets", "Localization", language.ToString(), "str_main");
            if (!Directory.Exists(stringsDir))
            {
                G.LogError($"stringsDir:{stringsDir} not Exists");
                return;
            }

            var files = Directory.GetFiles(stringsDir, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                sink.AddJson(File.ReadAllBytes(files[i]), files[i]);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// ILocalizationJsonSource:

        /// <summary>
        /// 读取语言名表与指定语言的字符串 JSON，逐份交给 sink。
        /// </summary>
        public void Load(Language language, ILocalizationJsonSink sink)
        {
            var assetsRoot = _assetsRoot;
            if (_resolveFromAppBase)
            {
                assetsRoot = FindAssetsRoot();
                if (assetsRoot == null)
                {
                    G.LogError("[Localization] HotAssets config dir not found");
                    return;
                }
            }

            LoadFiles(assetsRoot, language, sink);
        }
    }
}
