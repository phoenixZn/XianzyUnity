using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// 命令状态预处理器。实例走 SharedPool。
    /// </summary>
    public class EntityCmdPreHandler_CmdState : IEntityCommandPreHandler
    {
        public bool PreHandleCommand(LogicEntity owner, EntityCommand cmd)
        {
            return false;
        }

        public bool PreHandleSilentlyAndImmediately(LogicEntity owner, EntityCommand cmd)
        {
            return false; 
        }

        /// <summary>
        /// 按本类型归还 SharedPool。
        /// </summary>
        public void Recycle()
        {
            G.SharedPool.Return(this);
        }
    }
}
