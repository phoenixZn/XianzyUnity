namespace Xease.CoreGame
{
    /// <summary>
    /// 同类 Buff 合并规则：新 Buff 上身前决定与既有同 ID Buff 的取舍。
    /// </summary>
    public interface IBuffMergeRule
    {
        /// <summary>
        /// 合并规则，返回true表示保留当前buff（继续创建新buff）
        /// </summary>
        /// <param name="buffCenter">所在的buffCenter</param>
        /// <param name="genInfo">新buff的生成信息</param>
        bool Merge(BuffCenterComponent buffCenter, BuffGenInfo genInfo);
    }
}
