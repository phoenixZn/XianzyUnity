using System.Collections.Generic;

namespace Xease
{
    public partial class ServicesProvider
    {
        //框架性服务
        protected List<IService> _services = new();

        //外部挂接驱动
        public EnvDriver OuterDriver { get; } = new EnvDriver("Services");
        
        
        public virtual void Shutdown()
        {
            for (int i = _services.Count - 1; i >= 0; i--)
            {
                OuterDriver.UnBindEnvActions(_services[i]);
                _services[i].Shutdown();
            }
            OuterDriver.ClearAllBind();
        }

        public T AddService<T>(IService service, out T getter) where T : class
        {
            getter = service as T;
            if (getter == null)
            {
                return null;
            }
            _services.Add(service);
            OuterDriver.BindEnvActions(service);
            return getter;
        }
    }
}