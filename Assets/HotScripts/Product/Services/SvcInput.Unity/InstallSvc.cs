namespace Xease
{
    public static partial class G
    {
        public static IInputService Input => GEnv.Inst.Services.InputSvc;
    }

    public partial class ServicesProvider
    {
        protected IInputService _inputSvc;
        public IInputService InputSvc => _inputSvc;

        public void AddService_Input()
        {
            G.Log("AddService_Input");
            var svc = new InputService();
            AddService(svc, out _inputSvc);
            svc.LoadDevice(new StandardInputDevice());
            _inputSvc.Device.OnStartUp();
        }
    }
}
