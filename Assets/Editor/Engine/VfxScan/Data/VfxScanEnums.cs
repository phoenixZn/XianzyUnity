namespace Script.Editor.Extended.Engine.VfxScan
{
    public enum VfxEffectType
    {
        Default = 0,
        UI = 1,
        Ultimate = 2,
        Minion = 3,
        Scene = 4
    }

    public enum VfxHealthStatus
    {
        Pass = 0,
        Warning = 1,
        Error = 2
    }

    public enum VfxIssueCategory
    {
        ParticleCount = 0,
        MaxParticlesRedundant = 1,
        HighCostModule = 2,
        SimulationSpace = 3,
        MeshParticle = 4,
        MaterialCount = 5,
        ShaderRisk = 6,
        Sorting = 7,
        TextureSize = 8,
        TexturePot = 9,
        TextureMipmap = 10,
        TextureCompression = 11,
        Hierarchy = 12,
        RedundantComponent = 13,
        EmptyParticleSystem = 14
    }

    public enum VfxQuickFixKind
    {
        None = 0,
        DisableCollision = 1,
        DisableTrigger = 2,
        FixMaxParticles = 3,
        DisableMipmap = 4
    }

    public enum VfxMipmapPolicy
    {
        Any = 0,
        MustOff = 1,
        MustOn = 2
    }

    public enum VfxBudgetApplyMode
    {
        ByLabel = 0,
        ForceOverride = 1
    }
}
