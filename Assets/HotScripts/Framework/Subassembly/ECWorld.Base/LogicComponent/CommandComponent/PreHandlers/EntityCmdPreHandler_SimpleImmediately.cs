namespace Xease.CoreGame
{
    public class EntityCmdPreHandler_SimpleImmediately : IEntityCommandPreHandler
    {
        public bool PreHandleCommand(LogicEntity owner, EntityCommand cmd)
        {
            // 稀疏槽扫描，避免 GetComponents 冷缓存 ToArray 分配
            for (int i = 0, n = owner.totalComponents; i < n; i++)
            {
                if (!owner.HasComponent(i))
                    continue;
                var component = owner.GetComponent(i);
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
