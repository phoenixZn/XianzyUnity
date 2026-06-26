namespace Xease.CoreGame
{
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
    }
}
