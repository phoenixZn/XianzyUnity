namespace Xease
{
    public static partial class G
    {
        public static ITimerService Timer => GEnv.Inst.Services.TimerSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// 三级时间轮定时器：
        protected ITimerService _timerSvc;
        public ITimerService TimerSvc
        {
            get { return _timerSvc; }
        }

        public void AddService_Timer()
        {
            G.Log("AddService_Timer");
            var svc = new TimerService();
            AddService(svc, out _timerSvc);
        }
    }
}
