namespace Xease
{
    public static partial class G
    {
        public static IConfigService Config => GEnv.Inst.Services.ConfigSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// Luban 配表：
        protected IConfigService _configSvc; // Luban 配表服务实例
        public IConfigService ConfigSvc => _configSvc;

        /// <summary>
        /// 注册配表服务并同步加载；依赖 Asset RawFile 包，仅 Unity 宿主调用。
        /// </summary>
        public void AddService_Config()
        {
            G.Log("AddService_Config");
            var svc = new ConfigManager();
            svc.Init();
            AddService(svc, out _configSvc);
        }
    }
}
