namespace Xease
{
    public static partial class G
    {
        public static IValueEventService ValueEvent => GEnv.Inst.Services.ValueEventSvc;
    }
    
    public partial class ServicesProvider
    {
        protected IValueEventService _valueEventSvc;
        public IValueEventService ValueEventSvc
        {
            get { return _valueEventSvc; }
        }
        
        public void AddService_ValueEvent()
        {
            G.Log("AddService_ValueEvent");
            var svc = new ValueEventService();
            AddService(svc, out _valueEventSvc);
        }
    }
}