namespace Xease.CoreGame
{
    public class OwnerPlayerComponent : LogicComponent
    {
        public long UID { get; private set; }
        public InGamePlayerInfo PlayerInfoRef { get; private set; }

        public void Init(InGamePlayerInfo playerInfoRef)
        {
            PlayerInfoRef = playerInfoRef;
            UID = playerInfoRef.PlayerUID;
        }
    }

    public partial class LogicEntity
    {
        public OwnerPlayerComponent comOwnerPlayer
        {
            get { return (OwnerPlayerComponent)GetComponent(LogicComponentsLookup.ComOwnerPlayer); }
        }

        public bool hasComOwnerPlayer
        {
            get { return HasComponent(LogicComponentsLookup.ComOwnerPlayer); }
        }

        
        public void AddComOwnerPlayer(InGamePlayerInfo playerInfo)
        {
            var index = LogicComponentsLookup.ComOwnerPlayer;
            var component = (OwnerPlayerComponent)CreateComponent(index, typeof(OwnerPlayerComponent));
            component.Init(playerInfo);
            AddComponent(index, component);
        }
    }


    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComOwnerPlayerIndex = new(typeof(OwnerPlayerComponent));
        public static int ComOwnerPlayer => _ComOwnerPlayerIndex.Index;
    }
}