namespace Xease
{
    public enum EAssetGroup
    {
        // 手动管理单个资产生命周期
        Default = 0,

        // 以下按分组管理资产生命周期
        UI, // UI 预制体，随 IUIService.Shutdown 整组释放
    }
}