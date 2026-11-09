using System;
using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 整数血量与上限的混淆存储，数值变动通过组件替换通知观察者；死亡与数值范围由业务侧决定。
    /// </summary>
    public class HpComponent : LogicComponent
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 当前血量的混淆存储，挂载期间只通过本组件接口改写。
        private Obfuscated<long> _hp;
        // 血量上限的混淆存储，负值表示不使用有限上限。
        private Obfuscated<long> _maxHp;

        // 对外仅提供明文读值，避免调用方修改混淆结构体的副本。
        public long Hp => _hp.Value;
        // 上限不自动限制 Hp，调用方可自行处理超额血量。
        public long MaxHp => _maxHp.Value;

        /// <summary>
        /// 返回夹取到有效比例范围的血量比例；上限为负时视为满血，上限为零时返回零。
        /// </summary>
        public float HpPercent
        {
            get
            {
                var maxHp = MaxHp;
                if (maxHp < 0)
                    return 1f;
                if (maxHp == 0)
                    return 0f;

                // 先转浮点再相除，保留整数血量不足上限时的小数比例。
                return Mathf.Clamp01((float)((double)Hp / maxHp));
            }
        }

        /// <summary>
        /// 初始化当前血量 hp 与上限 maxHp；不夹取数值，也不发送组件变更通知。
        /// </summary>
        public void Init(long hp, long maxHp)
        {
            _hp = new Obfuscated<long>(hp);
            _maxHp = new Obfuscated<long>(maxHp);
        }

        /// <summary>
        /// 将当前血量改为 newHp，并在已挂载时发送组件变更通知；不触发死亡处理。
        /// </summary>
        public void SetHP(long newHp)
        {
            if (Hp == newHp)
                return;

            _hp.SetValue(newHp);
            NotifyChanged();
        }

        /// <summary>
        /// 将血量上限改为 newMaxHp，并在已挂载时发送组件变更通知；不调整当前血量。
        /// </summary>
        public void SetMaxHP(long newMaxHp)
        {
            if (MaxHp == newMaxHp)
                return;

            _maxHp.SetValue(newMaxHp);
            NotifyChanged();
        }

        /// <summary>
        /// 对当前血量应用 changeHp 增量，负值用于扣血；不夹取结果，也不触发死亡处理。
        /// </summary>
        /// <exception cref="OverflowException">结果超出整数血量的表示范围。</exception>
        public void ChangeHP(long changeHp)
        {
            SetHP(checked(Hp + changeHp));
        }

        // 初始化前或移除后允许独立改值，只有挂载期间才向实体派发通知。
        private void NotifyChanged()
        {
            _hostEntity?.ReplaceComponent(LogicComponentsLookup.ComHp, this);
        }

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        /// <summary>
        /// 移除时清理混淆存储，避免组件复用时保留上一实体的血量。
        /// </summary>
        public override void DisposeOnRemove()
        {
            _hp = default;
            _maxHp = default;
            base.DisposeOnRemove();
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        // 访问前需确认 hasComHp，保持与其他实体组件相同的访问约定。
        public HpComponent comHp => (HpComponent)GetComponent(LogicComponentsLookup.ComHp);
        // 供业务侧筛选具备血量能力的实体。
        public bool hasComHp => HasComponent(LogicComponentsLookup.ComHp);

        /// <summary>
        /// 添加或重设当前血量 hp 与上限 maxHp，并发送一次组件变更通知。
        /// </summary>
        public void SetComHp(long hp, long maxHp)
        {
            var index = LogicComponentsLookup.ComHp;
            if (hasComHp)
            {
                var current = comHp;
                current.Init(hp, maxHp);
                ReplaceComponent(index, current);
                return;
            }

            var component = (HpComponent)CreateComponent(index, typeof(HpComponent));
            component.Init(hp, maxHp);
            AddComponent(index, component);
        }

        /// <summary>
        /// 移除血量组件；尚未挂载时保持实体不变。
        /// </summary>
        public void RemoveComHp()
        {
            if (!hasComHp)
                return;

            RemoveComponent(LogicComponentsLookup.ComHp);
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        // 在当前组件注册表中为血量能力保留动态索引。
        private static ComponentTypeIndex _ComHpIndex = new(typeof(HpComponent));
        // 与实体组件访问和变更通知使用同一个索引。
        public static int ComHp => _ComHpIndex.Index;
    }
}
