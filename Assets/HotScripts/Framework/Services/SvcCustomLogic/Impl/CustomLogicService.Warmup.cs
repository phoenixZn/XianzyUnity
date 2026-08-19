namespace Xease.CoreGame
{
    public partial class CustomLogicService : ICustomLogicService
    {
        // 预热通用部件/节点池，避免运行时首次 new 抖动
        public void BaseWarmup()
        {
            WarmupParts_Base();
            WarmupNodes_Base();
        }

        // 预热 VarEnv 部件池，并预先创建热类型桶
        private void WarmupParts_Base()
        {
            _factory.PartsPool.Cache<VarEnv>(500, env => env.WarmupFastBuckets());
        }

        // 预热通用节点池（走 NodePool，与 CreateLogic/CreateCustomNode 同源）
        private void WarmupNodes_Base()
        {
            _factory.NodePool.Cache<CustomLogic>(32);
            _factory.NodePool.Cache<SequenceBhv>(128);
            _factory.NodePool.Cache<ParallelBhv>(32);
            _factory.NodePool.Cache<DelegateBhv>(256);
            _factory.NodePool.Cache<FTDelayBhv>(64);
            _factory.NodePool.Cache<ConditionBranchBhv>(16);
        }
    }
}
