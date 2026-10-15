namespace Xease.CoreGame
{
    public interface IHasSourceEntity
    {
        LogicEntity SourceEntity { get; }
    }

    // IHasOwnerEntity 已定义于 ECWorld.LiteUnity/LogicComponent/MainFSMComponent/CustomLogicGenInfoEx.cs，直接复用

    public interface IBuff
    {
        int Level { get; set; }
        int MaxLevel { get; set; }
        BuffGenInfo BuffGenInfo { get; }

        bool IsFinished();

        void UpdateBuff(float dt);

        void Upgrade();
    }
}
