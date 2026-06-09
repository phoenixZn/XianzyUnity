using Xease.CoreGame;

namespace Xease
{
    public static partial class G
    {
        public static ICustomLogicService CustomLogic => GEnv.Inst.Services.CustomLogicSvc;
    }
    
    public partial class ServicesProvider
    {
        protected ICustomLogicService _customLogicSvc;
        public ICustomLogicService CustomLogicSvc
        {
            get { return _customLogicSvc; }
        }
        
        public void AddService_CustomLogic()
        {
            G.Log("AddService_CustomLogic");
            var svc = new CustomLogicService();
            AddService(svc, out _customLogicSvc);
        }
    }
}