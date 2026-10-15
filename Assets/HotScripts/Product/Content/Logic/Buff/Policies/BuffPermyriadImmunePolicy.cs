namespace Xease.CoreGame
{
    /// <summary>
    /// 全体 Buff 的万分比免疫：读宿主免疫组件，满额则拒绝上身，否则按比例缩短持续时间。
    /// </summary>
    public sealed class BuffPermyriadImmunePolicy : IBuffAttachPolicy
    {
        //////////////////////////////////////////////////////////////////////////
        /// This:

        public static readonly BuffPermyriadImmunePolicy Instance = new(); // 无状态单例，由 Buff 配置容器注册

        private const int FullImmunePermyriad = 10000; // 万分比满额则拒绝上身

        private BuffPermyriadImmunePolicy() // 只通过 Instance 构造
        {
        }

        //////////////////////////////////////////////////////////////////////////
        /// IBuffAttachPolicy:

        /// <summary>
        /// 按宿主对本次 BuffId 与 BuffTag 的免疫值决定拒绝上身或缩短持续时间。
        /// </summary>
        public bool Apply(LogicEntity host, IEntityBuffCfg cfg, BuffGenInfo genInfo)
        {
            if (!host.hasComImmune)
                return true;

            var buffImmune = host.GetBuffTagImmune(cfg.BuffTag) + host.GetBuffImmune(genInfo.LogicConfigID);
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
