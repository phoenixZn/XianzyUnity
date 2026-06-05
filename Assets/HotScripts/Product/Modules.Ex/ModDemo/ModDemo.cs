namespace Xease
{
    [SkipModuleAutoRegister]
    public class ModDemo : Module
    {
        public ModDemo()
        {
            G.LogWarning("ModDemo 构造");
        }
        
        protected override void OnInit()
        {
            G.LogWarning("ModDemo OnInit");
            base.OnInit();
        }

        protected override void OnShutdown()
        {
            G.LogWarning("ModDemo OnShutdown");
            base.OnShutdown();
        }
    }
}