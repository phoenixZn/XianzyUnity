using UnityEngine;

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
        /// <summary>
        /// 注册协程服务，host 为协程运行的 MonoBehaviour 宿主（通常传 GEnvParam.UnityHost）
        /// </summary>
        public void AddService_Coroutine(MonoBehaviour host)
        {
            G.Log("AddService_Coroutine");
            AddService(new CoroutineService(host), out _coroutineSvc);
        }
    }

}