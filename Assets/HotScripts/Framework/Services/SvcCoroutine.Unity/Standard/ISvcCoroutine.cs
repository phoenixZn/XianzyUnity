using System.Collections;

namespace Xease
{

    //////////////////////////////////////////////////////////////////////////
    // Service: 协程
    public interface ICoroutineHandler
    {
        void Stop();
        void Pause();
        void Resume();
    }
    
    public interface ICoroutineService : IService
    {
        ICoroutineHandler StartCoroutine(IEnumerator coroutine);
        ICoroutineHandler StartCoroutine(object owner, IEnumerator coroutine);

        void StopOwnerCoroutines(object owner);
        void StopAllCoroutines();
    }
}