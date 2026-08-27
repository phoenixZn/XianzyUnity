namespace Xease.CoreGame
{
    public static partial class EntityExtension
    {
        public static InGamePlayerInfo GetPlayerInfo(this LogicEntity entity)
        {
            if (entity == null)
                return null;

            if (!entity.hasComOwnerPlayer)
            {
                if (entity.hasComHolder)
                {
                    var ownerEntity = entity.OwnerWorld.GetEntityWithComID(entity.comHolder.HolderEntityID);
                    return GetPlayerInfo(ownerEntity);
                }
                return null;
            }

            var comOwnerPlayer = entity.comOwnerPlayer;
            return comOwnerPlayer.PlayerInfoRef as InGamePlayerInfo;
        }
    }
}