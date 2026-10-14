using System;
using System.IO;

namespace Xease
{
    /// <summary>
    /// 从仓库 HotAssets 目录读取本地化 JSON。Unity 编辑器非播放与命令行宿主共用，不依赖资源包。
    /// </summary>
    internal static class LocalizationDiskSource
    {
        // 仓库 Assets 根；查找成功后复用
        private static string _assetsRoot;

        /// <summary>
        /// 从程序输出目录向上查找仓库 Assets 根目录。找不到返回 null。
        /// </summary>
        public static string FindAssetsRoot()
        {
            if (_assetsRoot != null)
            {
                return _assetsRoot;
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "HotAssets", "Config");
                if (Directory.Exists(candidate))
                {
                    _assetsRoot = Path.Combine(dir.FullName, "Assets");
                    return _assetsRoot;
                }

                dir = dir.Parent;
            }

            return null;
        }

        /// <summary>
        /// 读取语言名表与指定语言的字符串 JSON，逐份交给 consume。
        /// </summary>
        public static void Load(string assetsRoot, Language language, Action<byte[], string> consume)
        {
            if (string.IsNullOrEmpty(assetsRoot))
            {
                G.LogError($"Load assetsRoot IsNullOrEmpty");
                return;
            }

            var languageNamePath = Path.Combine(assetsRoot, "HotAssets", "Config", "App", "language.json");
            if (File.Exists(languageNamePath))
            {
                consume(File.ReadAllBytes(languageNamePath), languageNamePath);
            }

            var stringsDir = Path.Combine(assetsRoot, "HotAssets", "Config", "Localization", language.ToString(), "str_main");
            if (!Directory.Exists(stringsDir))
            {
                G.LogError($"stringsDir:{stringsDir} not Exists");
                return;
            }

            var files = Directory.GetFiles(stringsDir, "*.json");
            for (int i = 0; i < files.Length; i++)
            {
                consume(File.ReadAllBytes(files[i]), files[i]);
            }
        }
    }
}
