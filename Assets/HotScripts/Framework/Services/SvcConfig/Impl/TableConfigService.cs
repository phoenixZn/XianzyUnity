using System;
using cfg;
using Luban.SimpleJSON;

namespace Xease
{
    /// <summary>
    /// Luban 配表加载：用宿主注入的 loader 构造 <see cref="Tables"/>。
    /// 切二进制时只改 loader 的返回值类型，并改用 gen_client_bin。
    /// </summary>
    internal class TableConfigService : IConfigService
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 宿主注入的 JSON 加载；Tables 构造期间按无扩展文件名回调
        private readonly Func<string, JSONNode> _loadJson;

        /// <summary>
        /// 保存宿主提供的 JSON 加载方法，Init 时交给 Tables。
        /// </summary>
        public TableConfigService(Func<string, JSONNode> loadJson)
        {
            _loadJson = loadJson ?? throw new ArgumentNullException(nameof(loadJson));
        }

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
        /// 用构造时注入的 loader 同步构造表。
        /// </summary>
        public void Init()
        {
            Tables = new Tables(_loadJson);
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
        /// 丢弃 Tables。加载句柄由注入的 loader 在读取时释放。
        /// </summary>
        public void Shutdown()
        {
            Tables = null;
        }
    }
}
