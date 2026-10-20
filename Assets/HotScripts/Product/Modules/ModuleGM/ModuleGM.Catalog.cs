namespace Xease
{
    public partial class ModuleGM
    {
        // 功能块插装表。新增块只加一行；卸下时删掉对应类型和这一行
        void RegisterCatalogBlocks()
        {
            Register(new GmGameSpeed());
        }
    }
}
