namespace Xease.CoreGame
{
    public interface IEntityCommandPreHandler
    {
        bool PreHandleCommand(LogicEntity owner, EntityCommand cmd);
        bool PreHandleSilentlyAndImmediately(LogicEntity owner, EntityCommand cmd);
    }
}