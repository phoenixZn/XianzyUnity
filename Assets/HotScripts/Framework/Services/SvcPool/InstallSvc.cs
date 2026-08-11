namespace Xease
{
    public static partial class G
    {
        public static IPoolService SharedPool => GEnv.Inst.Services.SharedPoolSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// 通用共享对象池：
        protected IPoolService _sharedPoolSvc;
        public IPoolService SharedPoolSvc
        {
            get { return _sharedPoolSvc; }
        }
        public void AddService_SharedPool()
        {
            G.Log("AddService_SharedPool");
            var svc = new PoolService();
            AddService(svc, out _sharedPoolSvc);
        }
    }
}
