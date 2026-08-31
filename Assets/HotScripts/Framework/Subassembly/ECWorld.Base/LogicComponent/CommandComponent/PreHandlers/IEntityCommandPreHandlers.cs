namespace Xease.CoreGame
{
    /// <summary>
    /// 命令预处理器：发送队列出队前改写或立即执行。实例由 SharedPool 租还，CommandSender 移除时 Recycle。
    /// </summary>
    public interface IEntityCommandPreHandler
    {
        bool PreHandleCommand(LogicEntity owner, EntityCommand cmd);
        bool PreHandleSilentlyAndImmediately(LogicEntity owner, EntityCommand cmd);

        /// <summary>
        /// 归还 SharedPool；由 CommandSender 移除时调用。具体类型须 G.SharedPool.Return(this)。
        /// </summary>
        void Recycle();
    }
}
