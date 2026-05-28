using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace HotUpdate
{
    public interface ICoroutine
    {
    }

    //////////////////////////////////////////////////////////////////////////
    internal class CoroutineService : ICoroutineService, ICoroutine
    {
        public void Reset()
        {
        }

        private Dictionary<ICoroutine, List<CoroutineHandler>> _coroutineDict = new ();

        public ICoroutineHandler StartCoroutine(object owner, IEnumerator coroutine)
        {
            return StartCoroutine(owner as ICoroutine, coroutine);
        }
        
        public ICoroutineHandler StartCoroutine(IEnumerator coroutine)
        {
            return StartCoroutine(this, coroutine);
        }
        
        public ICoroutineHandler StartCoroutine(ICoroutine owner, IEnumerator coroutine)
        {
            CoroutineHandler handler = new CoroutineHandler(owner, coroutine, Remove);
            if (_coroutineDict.TryGetValue(owner, out var list))
            {
                list.Add(handler);
            }
            else
            {
                list = new List<CoroutineHandler>();
                list.Add(handler);
                _coroutineDict.Add(owner, list);
            }

            return handler;
        }

        private void Remove(CoroutineHandler handler)
        {
            var owner = handler.Owner;
            if (_coroutineDict.TryGetValue(owner, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0)
                {
                    _coroutineDict.Remove(owner);
                }
            }
        }
        
        public void StopOwnerCoroutines(object owner)
        {
            StopOwnerCoroutines(owner as ICoroutine);
        }
        public void StopOwnerCoroutines(ICoroutine owner)
        {
            if (_coroutineDict.TryGetValue(owner, out var list))
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    var item = list[i];
                    item.Stop();
                }
                _coroutineDict.Remove(owner);
            }
        }

        public void StopAllCoroutines()
        {
            var list = _coroutineDict.Values.ToList();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                var item = list[i];
                for (int j = item.Count - 1; j >= 0; j--)
                {
                    var item1 = item[j];
                    item1.Stop();
                }
            }
            _coroutineDict.Clear();
        }
    }
    
    public static class CoroutineExtension
    {
        public static ICoroutineHandler StartCoroutine(this ICoroutine owner, IEnumerator coroutine)
        {
            return GEnv.Inst.CoroutineSvc.StartCoroutine(owner, coroutine);
        }
        public static void StopAllCoroutines(this ICoroutine owner)
        {
            GEnv.Inst.CoroutineSvc.StopOwnerCoroutines(owner);
        }
    }
}