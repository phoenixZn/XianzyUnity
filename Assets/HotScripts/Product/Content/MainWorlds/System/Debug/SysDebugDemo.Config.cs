namespace Xease.CoreGame.Debug
{
    public partial class SysDebugDemo
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // MiniTemplate 示例表主键与期望字段，用于核对 Luban 生成物已加载
        private const int LubanSampleItemId = 1001;

        // 掉落表示例主键，核对嵌套池与 ResolveRef
        private const int LubanSampleDropId = 1;

        // 并行掉落示例主键
        private const int LubanSampleParallelDropId = 2;

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

        // 校验 TDropConfig / TDropPoolConfig：嵌套池、池引用与缺失键
        partial void TestLubanDrop()
        {
            var svc = G.Config;
            if (svc == null || !svc.Initialized)
            {
                G.LogError("TestLubanDrop fail: Config not initialized");
                return;
            }

            var drops = svc.Tables.TDropConfig;
            var poolTable = svc.Tables.TDropPoolConfig;
            if (drops.DataList.Count == 0 || poolTable.DataList.Count == 0)
            {
                G.LogError($"TestLubanDrop DataList empty: drop={drops.DataList.Count}, pool={poolTable.DataList.Count}");
                return;
            }

            var row = drops.GetOrDefault(LubanSampleDropId);
            if (row == null || row.UseParallelDrop || row.Pools == null || row.Pools.Count < 2)
            {
                G.LogError($"TestLubanDrop Get({LubanSampleDropId}) fail: {row}");
                return;
            }

            var first = row.Pools[0];
            var second = row.Pools[1];
            if (first.Pool_Ref == null
                || first.Pool_Ref.Id != first.Pool
                || first.Pool_Ref.Drops == null
                || first.Pool_Ref.Drops.Count == 0
                || first.DropLimit == 0
                || second.Pool_Ref == null
                || second.Pool_Ref.Id != second.Pool
                || second.Pool_Ref.Drops == null
                || second.Pool_Ref.Drops.Count == 0)
            {
                G.LogError($"TestLubanDrop ResolveRef fail: {row}");
                return;
            }

            var head = first.Pool_Ref.Drops[0];
            if (head.Weight == 0 || head.DropId == 0)
            {
                G.LogError($"TestLubanDrop drop entry fail: {head}");
                return;
            }

            var parallel = drops.GetOrDefault(LubanSampleParallelDropId);
            if (parallel == null || !parallel.UseParallelDrop || parallel.Pools == null || parallel.Pools.Count == 0)
            {
                G.LogError($"TestLubanDrop Get({LubanSampleParallelDropId}) fail: {parallel}");
                return;
            }

            if (drops.GetOrDefault(0) != null || poolTable.GetOrDefault(0) != null)
            {
                G.LogError("TestLubanDrop GetOrDefault missing key should be null");
                return;
            }

            G.Log($"TestLubanDrop ok, drop={drops.DataList.Count}, pool={poolTable.DataList.Count}, ref={first.Pool_Ref.Id}");
        }
    }
}
