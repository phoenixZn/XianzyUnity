using Entitas;

namespace Xease.CoreGame
{

    //////////////////////////////////////////////////////////////////////////
    /// EntityIndex: ComFaction
    public static partial class WorldExtension
    {
        public static void AddEntityIndex_ComFaction(this LogicWorld world)
        {
            var index = new EntityIndex<LogicEntity, EFaction>(
                "EntityIndex_Faction",
                world.GetGroup(LogicMatcher.AllOf(LogicComponentsLookup.ComFaction)),
                (e, c) => ((FactionComponent)c).Faction);
            world.AddEntityIndex(index);
        }
    }
}