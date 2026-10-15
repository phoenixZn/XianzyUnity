namespace Xease.CoreGame
{
    /// <summary>
    /// ImmuneComponent 的业务特化扩展：把 BoolImmune / PermyriadImmune 常量包装成语义化访问。
    /// </summary>
    public static class ImmuneComponentEx
    {
        // 布尔类型免疫效果
        public static bool InstantKill(this ImmuneComponent c) => c.GetBoolImmune(BoolImmune.InstantKill); // 秒杀
        public static bool SlowDown(this ImmuneComponent c) => c.GetBoolImmune(BoolImmune.SlowDown); // 减速
        public static bool Stun(this ImmuneComponent c) => c.GetBoolImmune(BoolImmune.Stun); // 无法行动（眩晕等）
        public static bool Pull(this ImmuneComponent c) => c.GetBoolImmune(BoolImmune.Pull); // 牵引
        public static bool Teleport(this ImmuneComponent c) => c.GetBoolImmune(BoolImmune.Teleport); // 传送

        // 万分比类型免疫效果
        public static int HitBack(this ImmuneComponent c) => c.GetPermyriadImmune(PermyriadImmune.HitBack); // 击退抗性
    }
}
