namespace HotUpdate.CoreGame
{
    public interface IEntityCommandPreHandler
    {
        bool PreHandleCommand(LogicEntity owner, EntityCommand cmd);
    }
}