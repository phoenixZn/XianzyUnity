namespace Xease
{
    public static partial class G
    {
        public static IGameObjectPoolService GameObjectPool_Core => GEnv.Inst.Services.GameObjectPoolCoreSvc;
        public static IGameObjectPoolService GameObjectPool_Battle => GEnv.Inst.Services.GameObjectPoolBattleSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// GameObject池：

        // Core 场景/常驻用 GameObject 池
        protected IGameObjectPoolService _gameObjectPoolCoreSvc;
        public IGameObjectPoolService GameObjectPoolCoreSvc => _gameObjectPoolCoreSvc;

        // Battle 战斗用 GameObject 池
        protected IGameObjectPoolService _gameObjectPoolBattleSvc;
        public IGameObjectPoolService GameObjectPoolBattleSvc => _gameObjectPoolBattleSvc;

        public void AddService_GameObjectPools()
        {
            G.Log("AddService_GameObjectPools");
            AddService(new GameObjectPoolService("[GameObjectPool.Core]"), out _gameObjectPoolCoreSvc);
            AddService(new GameObjectPoolService("[GameObjectPool.Battle]"), out _gameObjectPoolBattleSvc);
        }
    }
}
