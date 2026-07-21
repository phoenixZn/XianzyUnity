
namespace Xease.CoreGame
{

    public interface IHasLogicWorld
    {
        LogicWorld LogicWorld { get; }
    }
    
    public interface IHasMetaWorld
    {
        MetaWorld MetaWorld { get; }
    }

    public interface IHasOwnerPlayerInfo
    {
        InGamePlayerInfo OwnerPlayerInfo { get; }
    }

    public interface IHasOwnerEntity
    {
        LogicEntity OwnerEntity { get; }
    }

}