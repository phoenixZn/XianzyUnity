namespace Xease
{
    public static partial class G
    {
        public static ITimeService TickTime => GEnv.Inst.TickTime;

        public static float fixedDeltaTime => TickTime.fixedDeltaTime;
        public static float fixedUnscaledDeltaTime => TickTime.fixedUnscaledDeltaTime;
        public static float deltaTime => TickTime.deltaTime;
        public static float unscaledDeltaTime => TickTime.unscaledDeltaTime;
    }
    
    public partial class GEnv
    {
        protected ITimeService _tickTimeSvc;
        public ITimeService TickTime
        {
            get { return _tickTimeSvc; }
        }
        
        protected void AddService_TickTime()
        {
            G.Log("AddService_TickTime 1");
#if CONSOLE_CLIENT
            var svc = new TimeService_Console();
# else
            var svc = new TimeService_Unity();
#endif
            AddService(svc, out _tickTimeSvc);
        }
    }
}