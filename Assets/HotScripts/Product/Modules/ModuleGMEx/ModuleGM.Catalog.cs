namespace Xease
{
    public partial class ModuleGM
    {
        // 功能块插装表。在这里 Register，卸下时删掉对应类型和登记；要往块上补登记时先留住实例
        void RegisterCatalogBlocks()
        {
            Register(new GmGameSpeed());

            var staticAction = new GmStaticAction();
            Register(staticAction);
            
            // 示例按钮
            staticAction.Add("Test1", () => {G.Log("[GM] Test1"); });
            staticAction.Add("Test2", () => {G.Log("[GM] Test2"); });
        }

    }
}
