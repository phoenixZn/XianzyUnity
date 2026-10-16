namespace Xease
{
    public static partial class G
    {
        public static IGameObjectPoolService FxPool => GEnv.Inst.Services.FxPoolSvc;
        public static IGameObjectPoolService GOPool => GEnv.Inst.Services.GameObjectPoolSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// GameObject池：

        // Core 场景/常驻用 GameObject 池
        protected IGameObjectPoolService _fxPoolSvc;
        public IGameObjectPoolService FxPoolSvc => _fxPoolSvc;

        // Battle 战斗用 GameObject 池
        protected IGameObjectPoolService _gameObjectPoolSvc;
        public IGameObjectPoolService GameObjectPoolSvc => _gameObjectPoolSvc;

        public void AddService_GameObjectPools()
        {
            G.Log("AddService_GameObjectPools");
            AddService(new GameObjectPoolService("[FxPool]"), out _fxPoolSvc);
            AddService(new GameObjectPoolService("[GameObjectPool]"), out _gameObjectPoolSvc);
        }
    }
}
