using System.Collections.Generic;

namespace Xease.CoreGame
{
    using Nodes = List<ICustomNodeCfg>;


    //////////////////////////////////////////////////////////////////////////
    /// Demo Ex:
    public partial class LogicConfig_Buff
    {
        protected void InitConfigs_Demo()
        {
        }
        
        // 登记对容器内全部 Buff 生效的上身策略
        private void InitAttachPolicies()
        {
            _attachPolicies.Add(new DemoBuffPermyriadImmunePolicy());
        }
    }
    
    
    
    /// <summary>
    /// 全体 Buff 的万分比免疫：读宿主免疫组件，满额则拒绝上身，否则按比例缩短持续时间。
    /// </summary>
    public sealed class DemoBuffPermyriadImmunePolicy : IBuffAttachPolicy
    {
        //////////////////////////////////////////////////////////////////////////
        /// This:
        private const int FullImmunePermyriad = 10000; // 万分比满额则拒绝上身
        

        //////////////////////////////////////////////////////////////////////////
        /// IBuffAttachPolicy:

        /// <summary>
        /// 按宿主对本次 BuffId 与 BuffTag 的免疫值决定拒绝上身或缩短持续时间。
        /// </summary>
        public bool Apply(LogicEntity host, IEntityBuffCfg cfg, BuffGenInfo genInfo)
        {
            int buffImmune = 0;
            // if (!host.hasComImmune)
            //     return true;
            // buffImmune = host.GetBuffTagImmune(cfg.BuffTag) + host.GetBuffImmune(genInfo.LogicConfigID);
            if (buffImmune == 0)
                return true;

            if (buffImmune >= FullImmunePermyriad)
            {
                CLogger.LogDebug($"buff{genInfo.LogicConfigID} AddNewBuff 时 buff完全免疫 免疫值{buffImmune}");
                return false;
            }

            genInfo.DurationAddRate -= buffImmune / (float)FullImmunePermyriad;
            return true;
        }
    }
}
