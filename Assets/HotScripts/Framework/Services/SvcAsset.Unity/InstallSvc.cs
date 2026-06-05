namespace Xease
{
    public static partial class G
    {
        public static IAssetService Asset => GEnv.Inst.Services.AssetSvc;
    }
    
    public partial class ServicesProvider
    {
        protected IAssetService _assetSvc;
        public IAssetService AssetSvc
        {
            get { return _assetSvc; }
        }
        public void AddService_Asset()
        {
            G.Log("AddService_Asset");
            var svc = new AssetService();
            svc.Init(AppConfig.DefaultAssetPackageName, AppConfig.MainScriptRawPackageName);
            AddService(svc, out _assetSvc);
        }
    }
}