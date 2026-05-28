namespace HotUpdate.CoreGame
{
    public interface IEntityCommandDispatcher : IEntityCommandHandler
    {
        void BindOwner(LogicEntity owner);
        void UnBindOwner();
    }
}