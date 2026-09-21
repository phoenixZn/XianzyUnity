namespace Xease
{
    public static partial class G
    {
        public static IDeviceProfileService DeviceProfile => GEnv.Inst.Services.DeviceProfileSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// 硬件性能适配：
        protected IDeviceProfileService _deviceProfileSvc;
        public IDeviceProfileService DeviceProfileSvc
        {
            get { return _deviceProfileSvc; }
        }

        public void AddService_DeviceProfile()
        {
            G.Log("AddService_DeviceProfile");
            var svc = new DeviceProfileService();
            svc.Initialize();
            AddService(svc, out _deviceProfileSvc);
        }
    }
}
