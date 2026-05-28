namespace HotUpdate
{
    public static partial class G
    {
        public static IValueEventService ValueEvent => GEnv.Inst.ValueEvent;
    }
    
    public partial class GEnv
    {
        protected IValueEventService _valueEventSvc;
        public IValueEventService ValueEvent
        {
            get { return _valueEventSvc; }
        }
        protected void AddService_ValueEvent()
        {
            G.Log("AddService_ValueEvent");
            var svc = new ValueEventService();
            AddService(svc, out _valueEventSvc);
        }
    }
}