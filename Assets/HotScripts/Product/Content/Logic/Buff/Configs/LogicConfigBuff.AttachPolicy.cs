using System.Collections.Generic;

namespace Xease.CoreGame
{
    public partial class LogicConfig_Buff : IBuffAttachPolicyHost
    {
        //////////////////////////////////////////////////////////////////////////
        /// IBuffAttachPolicyHost:

        public IReadOnlyList<IBuffAttachPolicy> AttachPolicies => _attachPolicies;

        //////////////////////////////////////////////////////////////////////////
        /// This:

        private readonly List<IBuffAttachPolicy> _attachPolicies = new(); // 全体 Buff 共有上身策略，构造时填好

        // 登记对容器内全部 Buff 生效的上身策略
        private void InitAttachPolicies()
        {
            _attachPolicies.Add(BuffPermyriadImmunePolicy.Instance);
        }
    }
}
