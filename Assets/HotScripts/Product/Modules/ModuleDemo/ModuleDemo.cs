
namespace Xease
{
    // 演示模块，不参与自动注册
    public partial class ModuleDemo : Module, IEnvOnGUI
    {
        public ModuleDemo()
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