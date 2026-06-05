namespace Xease
{
    public static partial class G
    {
        public static IRandomService Random => GEnv.Inst.RandomSvc;
    }
    
    public partial class GEnv
    {
        protected IRandomService _randomSvc;
        public IRandomService RandomSvc
        {
            get { return _randomSvc; }
        }
        
        protected void AddService_Random(int seed)
        {
            G.Log($"AddService_Random seed={seed}");
            AddService(new RandomServicePCG(seed), out _randomSvc);
        }
    }
}
