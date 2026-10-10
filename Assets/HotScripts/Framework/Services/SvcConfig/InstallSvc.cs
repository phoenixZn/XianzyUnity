#if CONSOLE_CLIENT
using System;
using System.IO;
using Luban.SimpleJSON;
#else
using System;
using Luban.SimpleJSON;
using YooAsset;
#endif

namespace Xease
{
    public static partial class G
    {
        public static IConfigService Config => GEnv.Inst.Services.ConfigSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// Luban 配表：
        protected IConfigService _configSvc; // Luban 配表服务实例
        public IConfigService ConfigSvc => _configSvc;

#if CONSOLE_CLIENT
        // 仓库内 Luban JSON 目录；首次向上查找后复用
        private static string _lubanDataDir;

        /// <summary>
        /// 注册配表服务并同步加载；从仓库 JSON 目录读表。
        /// </summary>
        public void AddService_Config()
        {
            G.Log("AddService_Config");
            var svc = new TableConfigService(LoadJsonFromFile);
            svc.Init();
            AddService(svc, out _configSvc);
        }

        // 按无扩展文件名读取生成 JSON
        private static JSONNode LoadJsonFromFile(string name)
        {
            var path = Path.Combine(FindLubanDataDir(), name + ".json");
            if (!File.Exists(path))
            {
                throw new InvalidOperationException($"[Config] missing: {path}");
            }

            var text = File.ReadAllText(path);
            if (string.IsNullOrEmpty(text))
            {
                throw new InvalidOperationException($"[Config] empty: {name}");
            }

            return JSON.Parse(text);
        }

        // 从程序输出目录向上查找生成 JSON 所在目录
        private static string FindLubanDataDir()
        {
            if (_lubanDataDir != null)
            {
                return _lubanDataDir;
            }

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Assets", "HotAssets", "Config", "Luban");
                if (Directory.Exists(candidate))
                {
                    _lubanDataDir = candidate;
                    return _lubanDataDir;
                }

                dir = dir.Parent;
            }

            throw new InvalidOperationException("[Config] Luban json dir not found");
        }
#else
        /// <summary>
        /// 注册配表服务并同步加载；依赖已注册的 Asset RawFile 包。
        /// </summary>
        public void AddService_Config()
        {
            G.Log("AddService_Config");
            var svc = new TableConfigService(LoadJsonFromAsset);
            svc.Init();
            AddService(svc, out _configSvc);
        }

        // name 为无扩展文件名，与 AddressByFileName 一致；文本拷贝后释放句柄
        private static JSONNode LoadJsonFromAsset(string name)
        {
            var handle = G.Asset.LoadAssetRawFileSync(name);
            try
            {
                if (handle.Status != EOperationStatus.Succeed)
                {
                    throw new InvalidOperationException($"[Config] load failed: {name}, {handle.LastError}");
                }

                var text = handle.GetRawFileText();
                if (string.IsNullOrEmpty(text))
                {
                    throw new InvalidOperationException($"[Config] empty: {name}");
                }

                return JSON.Parse(text);
            }
            finally
            {
                G.Asset.Release(name);
            }
        }
#endif
    }
}
