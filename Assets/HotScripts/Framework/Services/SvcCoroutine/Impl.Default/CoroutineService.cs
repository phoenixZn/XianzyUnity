using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace Xease
{
    public interface ICoroutine
    {
    }

    //////////////////////////////////////////////////////////////////////////
    internal class CoroutineService : ICoroutineService, ICoroutine
#if CONSOLE_CLIENT
        , IEnvUpdate
#endif
    {
#if CONSOLE_CLIENT
        public CoroutineService()
        {
        }
#else
        // 协程运行的 MonoBehaviour 宿主，初始化时注入（通常为 GameEntry），生命周期与 GEnv 一致
        private readonly MonoBehaviour _host;

        public CoroutineService(MonoBehaviour host)
        {
            _host = host;
        }
#endif

        public void Shutdown()
        {
            // 服务关闭时停掉全部协程，避免 handler 泄漏与宿主上的空转协程
            StopAllCoroutines();
        }

        private Dictionary<ICoroutine, List<CoroutineHandler>> _coroutineDict = new ();
#if CONSOLE_CLIENT
        // CLI 帧泵列表：与 _coroutineDict 平行维护，EnvUpdate 倒序遍历驱动全部 handler
        private readonly List<CoroutineHandler> _tickingHandlers = new List<CoroutineHandler>();
#endif

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
#if CONSOLE_CLIENT
            CoroutineHandler handler = new CoroutineHandler(owner, coroutine, Remove);
            _tickingHandlers.Add(handler);
#else
            CoroutineHandler handler = new CoroutineHandler(owner, coroutine, Remove, _host);
#endif
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
#if CONSOLE_CLIENT
            _tickingHandlers.Remove(handler);
#endif
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

#if CONSOLE_CLIENT
        //////////////////////////////////////////////////////////////////////////
        /// IEnvUpdate:

        // CLI 帧泵：AddService 时由 ServicesProvider 自动 BindEnvActions 挂到 GEnv 驱动链；
        // 倒序遍历，Tick 内 Finish→Remove 自移除当前元素不影响索引
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            for (int i = _tickingHandlers.Count - 1; i >= 0; i--)
            {
                _tickingHandlers[i].Tick(dt);
            }
        }
#endif
    }
    
    public static class CoroutineExtension
    {
        public static ICoroutineHandler StartCoroutine(this ICoroutine owner, IEnumerator coroutine)
        {
            return G.Coroutines.StartCoroutine(owner, coroutine);
        }
        public static void StopAllCoroutines(this ICoroutine owner)
        {
            G.Coroutines.StopOwnerCoroutines(owner);
        }
    }
}