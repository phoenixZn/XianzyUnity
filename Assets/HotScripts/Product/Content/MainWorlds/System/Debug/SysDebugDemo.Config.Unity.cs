namespace Xease.CoreGame.Debug
{
    public partial class SysDebugDemo
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // MiniTemplate 示例表主键与期望字段，用于核对 Luban 生成物已加载
        private const int LubanSampleItemId = 1001;

        // 校验 Config 已 Init，Tbitem 行数、Get 字段与缺失键 GetOrDefault
        partial void TestLubanConfig()
        {
            var svc = G.Config;
            if (svc == null || !svc.Initialized)
            {
                G.LogError("TestLubanConfig fail: Config not initialized");
                return;
            }

            var tb = svc.Tables.Tbitem;
            if (tb.DataList.Count == 0)
            {
                G.LogError($"TestLubanConfig DataList count fail: {tb.DataList.Count}");
                return;
            }

            var item = tb.GetOrDefault(LubanSampleItemId);
            if (item == null
                || string.IsNullOrEmpty(item.Name)
                || string.IsNullOrEmpty(item.Desc)
                || item.Count == 0)
            {
                G.LogError($"TestLubanConfig Get({LubanSampleItemId}) fail: {item}");
                return;
            }

            if (tb.GetOrDefault(0) != null)
            {
                G.LogError("TestLubanConfig GetOrDefault missing key should be null");
                return;
            }

            G.Log($"TestLubanConfig ok, count={tb.DataList.Count}, {item.Id}:{item.Name}");
        }
    }
}
