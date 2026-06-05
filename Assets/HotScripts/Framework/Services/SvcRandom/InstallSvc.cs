namespace Xease
{
    public static partial class G
    {
        public static IRandomService Random => GEnv.Inst.Services.RandomSvc;
    }
    
    public partial class ServicesProvider
    {
        protected IRandomService _randomSvc;
        public IRandomService RandomSvc
        {
            get { return _randomSvc; }
        }
        
        public void AddService_Random(int seed)
        {
            G.Log($"AddService_Random seed={seed}");
            AddService(new RandomServicePCG(seed), out _randomSvc);
        }
    }
}
