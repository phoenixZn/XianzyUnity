using System.Collections.Generic;

namespace Xease
{
    public class ServicesProvider
    {
        //框架性服务
        protected List<IService> _services = new();
        
        
        public virtual void Shutdown()
        {
            for (int i = _services.Count - 1; i >= 0; i--)
            {
                _services[i].Shutdown();
            }
        }
        
        public virtual void Update(float deltaTime, float unscaledDeltaTime)
        {
            foreach (IService svc in _services)
            {
                if (svc is IEnvTick tickSvc)
                {
                    tickSvc.Update(deltaTime, unscaledDeltaTime);
                }
            }
        }

        public T AddService<T>(IService service, out T getter) where T : class
        {
            getter = service as T;
            if (getter == null)
            {
                return null;
            }
            _services.Add(service);
            return getter;
        }
    }
}