namespace Xease.CoreGame
{
    public class EntityCmdPreHandler_SimpleImmediately : IEntityCommandPreHandler
    {
        public bool PreHandleCommand(LogicEntity owner, EntityCommand cmd)
        {
            foreach (var component in owner.GetComponents())
            {
                if (component is IEntityCommandHandler commandHandler)
                {
                    commandHandler.HandleEntityCommand(owner, cmd);
                }
            }
            return true;
        }

        public bool PreHandleSilentlyAndImmediately(LogicEntity owner, EntityCommand cmd)
        {
            //相当于所有命令都先通过PreHandle 当前帧直接执行。 CommandSenderComponent永远静默
            return PreHandleCommand(owner, cmd); 
        }
    }
}
