namespace Xease.CoreGame
{
    /// <summary>
    /// LogicEntity 的免疫语义化访问：无免疫组件时一律视为不免疫。
    /// </summary>
    public static class LogicEntityImmuneEx
    {
        public static bool IsImmuneToInstantKill(this LogicEntity e) => e.hasComImmune && e.comImmune.InstantKill(); // 秒杀
        public static bool IsImmuneToSlowDown(this LogicEntity e) => e.hasComImmune && e.comImmune.SlowDown(); // 减速
        public static bool IsImmuneToStun(this LogicEntity e) => e.hasComImmune && e.comImmune.Stun(); // 无法行动（眩晕等）
        public static bool IsImmuneToPull(this LogicEntity e) => e.hasComImmune && e.comImmune.Pull(); // 牵引
        public static bool IsImmuneToTeleport(this LogicEntity e) => e.hasComImmune && e.comImmune.Teleport(); // 传送

        public static int ImmuneToHitBack(this LogicEntity e) => e.hasComImmune ? e.comImmune.HitBack() : 0; // 击退抗性
    }
}
