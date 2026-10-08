namespace Xease.UI
{
    /// <summary>
    /// UI 层标识；枚举整型值同时作为 UIManager 层列表下标。
    /// </summary>
    public enum UILayerId
    {
        Hud = 0,
        Panel = 1,
        Pop = 2,
        Top = 3,
        OverTop = 4, // 奖励、解锁等盖在常规弹窗之上
        Fix = 5,
    }
}
