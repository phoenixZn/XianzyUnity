namespace Xease.CoreGame
{
    /// <summary>
    /// 条件修饰符的判定。conditionId 的含义由游戏定义。
    /// 用结构体实现时，Query 不会把上下文装箱。
    /// 判定过程中不要读写同一个 StatComponent。
    /// </summary>
    public interface IStatContext
    {
        /// <summary>该条件当前是否成立。</summary>
        bool Test(int conditionId);
    }

    public sealed partial class StatComponent
    {
        /// <summary>
        /// 把无条件修饰符与本次上下文放行的条件修饰符合成该属性。
        /// 没有条件修饰符时与 <see cref="Get"/> 相同。
        /// </summary>
        public long Query<TContext>(int statId, TContext context) where TContext : IStatContext
        {
            if (!CheckStat(statId))
                return 0;
            if (_conditionalCount == 0)
                return Get(statId);
            return Fold(statId, context, true);
        }
    }
}
