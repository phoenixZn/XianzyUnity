namespace Xease
{
    public static partial class G
    {
        public static ICoroutineService Coroutines => GEnv.Inst.Services.CoroutineSvc;
    }
    
    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        // Service：协程
        protected ICoroutineService _coroutineSvc;
        public ICoroutineService CoroutineSvc
        {
            get { return _coroutineSvc; }
        }
        public void AddService_Coroutine()
        {
            G.Log("AddService_Coroutine");
            AddService(new CoroutineService(), out _coroutineSvc);
        }
    }

}