using Xease.UI;

namespace Xease
{
    public static partial class G
    {
        public static IUIService UI => GEnv.Inst.Services.UISvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// UI 服务：
        protected IUIService _uiSvc;
        public IUIService UISvc => _uiSvc;

        /// <summary>
        /// 注册 UI 服务；依赖 Asset、Coroutine，仅 Unity 宿主调用。
        /// </summary>
        public void AddService_UI()
        {
            G.Log("AddService_UI");
            var svc = new UIManager();
            svc.Init();
            AddService(svc, out _uiSvc);
        }
    }
}
