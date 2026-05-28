namespace HotUpdate.CoreGame
{
    public static partial class EntityExtension
    {
        public static LogicEntity GetEntity(this LogicWorld world, long id)
        {
            return world.GetEntityWithComID(id);
        }
    }
}