using UnityEngine;

namespace Xease
{
    public partial class ModuleDebug : Module
    {
        public ModuleDebug()
        {
        }
        
        protected override void OnInit()
        {
            G.Log("ModuleDebug 构造");
            base.OnInit();
        }

        protected override void OnShutdown()
        {
            base.OnShutdown();
        }
        
    }
}