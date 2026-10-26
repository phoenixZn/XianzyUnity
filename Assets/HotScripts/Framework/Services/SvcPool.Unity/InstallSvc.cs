namespace Xease
{
    public static partial class G
    {
        public static IGameObjectPoolService GOPool => GEnv.Inst.Services.GameObjectPoolSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// GameObject池：


        // GameObjec对象池
        protected IGameObjectPoolService _gameObjectPoolSvc;
        public IGameObjectPoolService GameObjectPoolSvc => _gameObjectPoolSvc;

        public GameObjectPoolService AddService_GameObjectPools()
        {
            G.Log("AddService_GameObjectPools");
            var svc = new GameObjectPoolService("[GameObjectPool]");
            AddService(svc, out _gameObjectPoolSvc);
            return svc;
        }
    }
}
