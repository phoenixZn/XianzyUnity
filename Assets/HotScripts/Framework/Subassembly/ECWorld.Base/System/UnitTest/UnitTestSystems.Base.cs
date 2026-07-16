using Entitas;

namespace Xease.CoreGame
{
    public class UnitTestSystems_Base : Systems
    {
        public UnitTestSystems_Base(ECWorlds worlds)
        {
            Add(new UnitTestSystem_Tag(worlds));
        }
    }
}