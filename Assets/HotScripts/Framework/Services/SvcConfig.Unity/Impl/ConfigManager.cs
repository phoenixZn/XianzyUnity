using System;
using cfg;
using Luban.SimpleJSON;
using YooAsset;

namespace Xease
{
    /// <summary>
    /// Luban 配表加载：从 PackMainScript RawFile 同步读 JSON，注入 <see cref="Tables"/> 构造器。
    /// 切二进制时只改 loader 返回值为 <c>ByteBuf</c>，并改用 gen_client_bin。
    /// </summary>
    internal class ConfigManager : IConfigService
    {
        // 配置 RawFile 句柄；Init 创建，Shutdown 释放
        private AssetLoader _loader;

        //////////////////////////////////////////////////////////////////////////
        /// IConfigService:

        /// <summary>
        /// 已加载的 Luban 表集合；未 Init 时为 null。
        /// </summary>
        public Tables Tables { get; private set; }

        /// <summary>
        /// 表是否已成功构造。
        /// </summary>
        public bool Initialized => Tables != null;

        /// <summary>
        /// 按 Tables 所需文件名从 RawFile 包同步加载 JSON 并构造表。
        /// </summary>
        public void Init()
        {
            _loader?.Release();
            _loader = new AssetLoader();
            Tables = new Tables(LoadJson);
            var items = Tables.Tbitem.DataList;
            if (items.Count == 0)
            {
                G.Log("[Config] Tables ready, Tbitem empty");
                return;
            }

            var first = items[0];
            G.Log($"[Config] Tables ready, Tbitem count={items.Count}, first={first.Id}:{first.Name}");
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService:

        /// <summary>
        /// 丢弃 Tables 并释放配置 RawFile。
        /// </summary>
        public void Shutdown()
        {
            Tables = null;
            _loader?.Release();
            _loader = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        // Tables 构造器回调：name 为无扩展文件名，与 AddressByFileName 一致
        private JSONNode LoadJson(string name)
        {
            var handle = _loader.LoadAssetRawFileSync(name);
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
    }
}
