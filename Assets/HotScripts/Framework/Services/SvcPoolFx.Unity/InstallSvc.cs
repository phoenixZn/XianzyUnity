namespace Xease
{
    public static partial class G
    {
        public static IFxPoolService FxPool => GEnv.Inst.Services.FxPoolSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// GameObject池：

        // 特效池；租还走 GameObject 池。后注册，Shutdown 时先归还特效
        protected IFxPoolService _fxPoolSvc;
        public IFxPoolService FxPoolSvc => _fxPoolSvc;
        

        public void AddService_FxPool(IGameObjectPoolService gameObjectPoolSvc)
        {
            if (gameObjectPoolSvc == null)
                G.LogError("AddService_FxPool gameObjectPoolSvc == null");
            
            G.Log("AddService_FxPool");
            AddService(new FxPoolService(gameObjectPoolSvc), out _fxPoolSvc);
        }
    }
}