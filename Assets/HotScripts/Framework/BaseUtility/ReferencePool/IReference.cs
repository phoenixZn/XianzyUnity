namespace HotUpdate
{
    /// <summary>
    /// 引用接口。
    /// </summary>
    public interface IReference
    {
        /// <summary>
        /// 清理引用。
        /// </summary>
        void OnRecycle();
    }
}