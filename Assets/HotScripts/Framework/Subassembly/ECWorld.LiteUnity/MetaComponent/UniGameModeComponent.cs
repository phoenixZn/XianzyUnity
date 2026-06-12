namespace Xease.CoreGame
{
    public class UniGameModeComponent : MetaComponent
    {
        public CustomLogic GameModeLogic { get; private set; }

        public void Init(ICustomLogicGenInfo genInfo)
        {
            GameModeLogic = G.CustomLogic.CreateLogic<CustomLogic>(genInfo);
        }

        public override void DisposeOnRemove()
        {
            if (GameModeLogic != null)
            {
                G.CustomLogic.DestroyLogic(GameModeLogic);
                GameModeLogic = null;
            }
            base.DisposeOnRemove();
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class MetaWorld
    {
        public UniGameModeComponent comUniGameMode
        {
            get { return GetUniqueComponent<UniGameModeComponent>(MetaComponentsLookup.ComUniGameMode); }
        }

        public bool hasComUniGameMode
        {
            get { return HasUniqueComponent(MetaComponentsLookup.ComUniGameMode); }
        }
        
        public void SetComUniGameMode(ICustomLogicGenInfo genInfo)
        {
            var index = MetaComponentsLookup.ComUniGameMode;
            var component = (UniGameModeComponent)UniqueEntity.CreateComponent(index, typeof(UniGameModeComponent));
            component.Init(genInfo);
            SetUniqueComponent(index, component);
        }
        
        public void RemoveComUniGameMode()
        {
            if (hasComUniGameMode)
            {
                UniqueEntity.RemoveComponent(MetaComponentsLookup.ComUniGameMode);    
            }
        }
        
    }

    public static partial class MetaComponentsLookup
    {
        private static ComponentTypeIndex _ComUniGameModeIndex = new(typeof(UniGameModeComponent));
        public static int ComUniGameMode => _ComUniGameModeIndex.Index;
    }
}