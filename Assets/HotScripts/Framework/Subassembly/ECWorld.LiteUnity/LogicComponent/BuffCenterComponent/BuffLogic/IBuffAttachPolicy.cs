using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary>
    /// 全体 Buff 上身前的共有策略：可拒绝上身，或改写 <see cref="BuffGenInfo"/>。
    /// 由配置容器按注册顺序提供，<see cref="BuffCenterComponent"/> 只负责调度。
    /// </summary>
    public interface IBuffAttachPolicy
    {
        /// <summary>
        /// 对即将上身的 Buff 执行共有规则。返回 false 表示拒绝上身；允许改写 genInfo。
        /// </summary>
        /// <param name="host">承受 Buff 的宿主实体</param>
        /// <param name="cfg">该 Buff 的配置</param>
        /// <param name="genInfo">本次生成信息，策略可改写其中的运行时初始数据</param>
        bool Apply(LogicEntity host, IEntityBuffCfg cfg, BuffGenInfo genInfo);
    }

    /// <summary>
    /// 持有全体 Buff 共有上身策略的配置容器。未实现该接口的容器在上身流程中跳过共有策略。
    /// </summary>
    public interface IBuffAttachPolicyHost
    {
        /// <summary>按注册顺序执行；列表在配置构造完成后只读。</summary>
        IReadOnlyList<IBuffAttachPolicy> AttachPolicies { get; }
    }
}
