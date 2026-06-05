namespace Xease
{
    public static partial class G
    {
        public static ICoroutineService Coroutines => GEnv.Inst.CoroutineSvc;
    }
    
    public partial class GEnv
    {
        //////////////////////////////////////////////////////////////////////////
        // Service：协程
        protected ICoroutineService _coroutineSvc;
        internal ICoroutineService CoroutineSvc
        {
            get { return _coroutineSvc; }
        }
        protected void AddService_Coroutine()
        {
            G.Log("AddService_Coroutine");
            AddService(new CoroutineService(), out _coroutineSvc);
        }
    }

}