namespace Xease.CoreGame
{
    public static partial class NodeConfigTypeRegistry
    {
        static bool _ImmuneBhvCfg = Register(typeof(ImmuneBhvCfg), NodeCategory.Bhv);
    }

    /// <summary>
    /// 免疫行为配置：节点开始时给宿主写入一路免疫，销毁时按同一 Flag 移除。
    /// </summary>
    public class ImmuneBhvCfg : ICustomNodeCfg
    {
        /// <summary>
        /// 免疫写入种类，对应 ImmuneComponent 的四类来源。
        /// </summary>
        public enum ImmuneTypeEnum
        {
            Bool = 1, // 布尔免疫
            Permyriad = 2, // 万分比免疫
            BuffId = 3, // 指定 Buff 免疫
            BuffTag = 4, // Buff 标签免疫
        }

        public int Flag; // 来源标记，与 ImmuneComponent 的 flag 成对添加和移除
        public ImmuneTypeEnum ImmuneType; // 写入哪一类免疫
        public int ImmuneId; // 免疫条目：布尔/万分比类型、BuffId 或 BuffTag
        public int ImmuneValue; // 万分比免疫值；布尔类型不使用

        //////////////////////////////////////////////////////////////////////////
        /// ICustomNodeCfg:

        /// <summary>
        /// 返回运行时节点类型。
        /// </summary>
        public System.Type NodeType()
        {
            return typeof(ImmuneBhv);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        /// <summary>
        /// 以指定来源标记创建配置，需再链式指定免疫种类。
        /// </summary>
        public ImmuneBhvCfg(int flag)
        {
            Flag = flag;
        }

        /// <summary>
        /// 配置布尔免疫。id 为布尔免疫类型，写入值固定为 true，返回自身以便链式调用。
        /// </summary>
        public ImmuneBhvCfg WithBool(int id)
        {
            ImmuneType = ImmuneTypeEnum.Bool;
            ImmuneId = id;
            return this;
        }

        /// <summary>
        /// 配置万分比免疫。id 为万分比免疫类型，value 为免疫值，返回自身以便链式调用。
        /// </summary>
        public ImmuneBhvCfg WithPermyriad(int id, int value)
        {
            ImmuneType = ImmuneTypeEnum.Permyriad;
            ImmuneId = id;
            ImmuneValue = value;
            return this;
        }

        /// <summary>
        /// 配置对指定 Buff 的免疫。value 为免疫值，返回自身以便链式调用。
        /// </summary>
        public ImmuneBhvCfg WithBuffId(int buffId, int value)
        {
            ImmuneType = ImmuneTypeEnum.BuffId;
            ImmuneId = buffId;
            ImmuneValue = value;
            return this;
        }

        /// <summary>
        /// 配置对指定 BuffTag 的免疫。value 为免疫值，返回自身以便链式调用。
        /// </summary>
        public ImmuneBhvCfg WithBuffTag(int buffTag, int value)
        {
            ImmuneType = ImmuneTypeEnum.BuffTag;
            ImmuneId = buffTag;
            ImmuneValue = value;
            return this;
        }
    }

    /// <summary>
    /// 运行时免疫行为：OnBegin 给宿主挂上配置的免疫，Destroy 时按 Flag 卸下。
    /// </summary>
    public class ImmuneBhv : BehaviorNode<ImmuneBhvCfg>
    {
        //////////////////////////////////////////////////////////////////////////
        /// BehaviorNodeBase：override

        protected override void OnBegin()
        {
            base.OnBegin();
            if (mCfg == null)
            {
                return;
            }

            var entity = this.GetOwnerEntity();
            if (entity == null)
            {
                return;
            }

            if (!entity.hasComImmune)
            {
                entity.AddComImmune();
            }

            // 按配置种类写入对应免疫来源
            switch (mCfg.ImmuneType)
            {
                case ImmuneBhvCfg.ImmuneTypeEnum.Bool:
                    entity.comImmune.AddBoolImmune(mCfg.ImmuneId, true, mCfg.Flag);
                    break;
                case ImmuneBhvCfg.ImmuneTypeEnum.Permyriad:
                    entity.comImmune.AddPermyriadImmune(mCfg.ImmuneId, mCfg.ImmuneValue, mCfg.Flag);
                    break;
                case ImmuneBhvCfg.ImmuneTypeEnum.BuffId:
                    entity.comImmune.AddBuffImmune(mCfg.ImmuneId, mCfg.ImmuneValue, mCfg.Flag);
                    break;
                case ImmuneBhvCfg.ImmuneTypeEnum.BuffTag:
                    entity.comImmune.AddBuffTagImmune(mCfg.ImmuneId, mCfg.ImmuneValue, mCfg.Flag);
                    break;
            }
        }

        /// <summary>
        /// 按配置种类和 Flag 卸下本节点写入的免疫。
        /// </summary>
        public override void Destroy()
        {
            if (mCfg != null)
            {
                var entity = this.GetOwnerEntity();
                if (entity != null && entity.hasComImmune)
                {
                    switch (mCfg.ImmuneType)
                    {
                        case ImmuneBhvCfg.ImmuneTypeEnum.Bool:
                            entity.comImmune.RemoveBoolImmune(mCfg.ImmuneId, mCfg.Flag);
                            break;
                        case ImmuneBhvCfg.ImmuneTypeEnum.Permyriad:
                            entity.comImmune.RemovePermyriadImmune(mCfg.ImmuneId, mCfg.Flag);
                            break;
                        case ImmuneBhvCfg.ImmuneTypeEnum.BuffId:
                            entity.comImmune.RemoveBuffImmune(mCfg.ImmuneId, mCfg.Flag);
                            break;
                        case ImmuneBhvCfg.ImmuneTypeEnum.BuffTag:
                            entity.comImmune.RemoveBuffTagImmune(mCfg.ImmuneId, mCfg.Flag);
                            break;
                    }
                }
            }

            base.Destroy();
        }
    }
}
