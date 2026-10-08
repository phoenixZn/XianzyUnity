namespace Xease.UI
{
    /// <summary>
    /// 随 Panel/View 的 Hide 或 Destroy 被清理的句柄。
    /// </summary>
    public interface ICloseClear
    {
        /// <summary>
        /// 所属 UI 关闭或销毁时调用。
        /// </summary>
        void ClearOnClose();
    }
}
