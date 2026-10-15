using System.Collections.Generic;

namespace Xease.CoreGame
{
    /// <summary>
    /// Buff 中心组件：持有实体身上的全部 Buff，负责按配置与合并规则创建 Buff（Update 期间入队延迟落地）、
    /// 逐帧驱动 Buff Update、按 ID/Tag 移除，并把实体命令转发给各 Buff。
    /// </summary>
    public sealed class BuffCenterComponent : LogicComponent, IEntityCommandHandler
    {
        private List<IBuff> _buffs = new List<IBuff>();
        // Update 期间新挂的 Buff 先入队，由 FlushPendingBuffs 统一落地，避免遍历中修改集合
        private List<BuffGenInfo> _creatingList = new List<BuffGenInfo>();
        // Update 递归深度，大于 0 时 AddNewBuff 走延迟创建队列
        private int _updateDepth;

        public List<IBuff> BuffList
        {
            get { return _buffs; }
            private set { _buffs = value; }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 查询

        public bool HasBuff(int logicConfigId)
        {
            for (int i = 0; i < _buffs.Count; i++)
            {
                if (_buffs[i].BuffGenInfo.LogicConfigID == logicConfigId)
                    return true;
            }
            return false;
        }

        public bool HasBuffAtMaxLevel(int logicConfigId)
        {
            var buff = GetBuff(logicConfigId);
            return buff != null && buff.MaxLevel > 0 && buff.Level >= buff.MaxLevel;
        }

        /// <summary>
        /// 取同 ID Buff 中最后创建的一个（叠层规则下取最新实例）。
        /// </summary>
        public BuffLogic GetBuff(int logicConfigId)
        {
            BuffLogic last = null;
            for (int i = 0; i < _buffs.Count; i++)
            {
                var buff = _buffs[i] as BuffLogic;
                if (buff != null && buff.GenInfo.LogicConfigID == logicConfigId)
                    last = buff;
            }
            return last;
        }

        /// <summary>
        /// 同 ID Buff 的层数总和（叠层规则下每个实例单独计层）。
        /// </summary>
        public int GetBuffSumLevel(int logicConfigId)
        {
            int sum = 0;
            for (int i = 0; i < _buffs.Count; i++)
            {
                var b = _buffs[i];
                if (b.BuffGenInfo.LogicConfigID == logicConfigId)
                    sum += b.Level;
            }
            return sum;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 创建与移除

        /// <summary>
        /// 施加新 Buff。依次经过配置校验、TargetChecker 预检测、容器共有上身策略、同类合并规则；
        /// Update 期间调用时入待创建队列延迟落地。返回 false 表示 Buff 未上身。
        /// 一旦传入，无论成败调用方都不再 Destroy GenInfo。
        /// </summary>
        public bool AddNewBuff(BuffGenInfo genInfo)
        {
            var container = G.CustomLogic.GetConfigContainer(genInfo.ConfigContainerName);
            var cfg = container?.GetCustomLogicCfg(genInfo.LogicConfigID) as IEntityBuffCfg;
            if (cfg == null)
            {
                G.LogWarning($"buff{genInfo.LogicConfigID} AddNewBuff cfg不是 IEntityBuffCfg");
                RecycleUnusedGenInfo(genInfo);
                return false;
            }

            // buff条件预检测
            if (cfg.TargetChecker != null && !cfg.TargetChecker(HostEntity))
            {
                G.LogWarning($"buff{genInfo.LogicConfigID} AddNewBuff 时 TargetChecker 检查不通过");
                RecycleUnusedGenInfo(genInfo);
                return false;
            }

            if (!ApplyAttachPolicies(container, cfg, genInfo))
                return false;

            // 同类buff合并规则
            var mergeRule = cfg.MergeRule ?? BuffMergeRule.Replace; // 默认为Replace规则
            if (!mergeRule.Merge(this, genInfo))
            {
                CLogger.LogDebug($"buff{genInfo.LogicConfigID} AddNewBuff 合并规则 {mergeRule.GetType().Name}");
                RecycleUnusedGenInfo(genInfo);
                return false;
            }

            genInfo.OwnerEntity = HostEntity;

            if (_updateDepth > 0)
            {
                _creatingList.Add(genInfo);
                return true;
            }

            CreateAndAttachBuff(genInfo);
            return true;
        }

        // 容器未实现 IBuffAttachPolicyHost 时跳过。任一策略拒绝则回收 GenInfo。
        private bool ApplyAttachPolicies(ILogicConfigContainer container, IEntityBuffCfg cfg, BuffGenInfo genInfo)
        {
            if (container is not IBuffAttachPolicyHost host)
                return true;

            var policies = host.AttachPolicies;
            for (int i = 0; i < policies.Count; i++)
            {
                if (policies[i].Apply(HostEntity, cfg, genInfo))
                    continue;

                CLogger.LogDebug($"buff{genInfo.LogicConfigID} AddNewBuff 共有策略 {policies[i].GetType().Name} 拒绝上身");
                RecycleUnusedGenInfo(genInfo);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 将待创建队列中的 Buff 立刻落到 m_buffs；落地过程可能链式触发新的入队，限量多趟直到清空。
        /// </summary>
        public void FlushPendingBuffs()
        {
            const int maxPass = 8; // 安全趟数上限，防止 Buff 落地时链式创建新 Buff 导致死循环
            for (int pass = 0; pass < maxPass && _creatingList.Count > 0; pass++)
            {
                var pending = _creatingList;
                _creatingList = new List<BuffGenInfo>();
                for (int i = 0; i < pending.Count; i++)
                {
                    CreateAndAttachBuff(pending[i]);
                }
            }

            if (_creatingList.Count > 0)
            {
                G.LogWarning($"FlushPendingBuffs 超过安全次数仍有待创建 Buff，count={_creatingList.Count}");
                RecycleCreatingList();
            }
        }

        private void CreateAndAttachBuff(BuffGenInfo genInfo)
        {
            var buffLogic = G.CustomLogic.CreateLogic<BuffLogic>(genInfo);
            if (buffLogic == null)
            {
                RecycleUnusedGenInfo(genInfo);
                return;
            }

            _buffs.Add(buffLogic);
            buffLogic.OnEntityAddBuff(HostEntity);
        }

        /// <summary>
        /// 移除所有指定LogicID的buff
        /// </summary>
        public bool RemoveBuffByID(params int[] logicConfigId)
        {
            if (logicConfigId == null || logicConfigId.Length == 0) return false;
            bool removed = false;
            for (int i = _buffs.Count - 1; i >= 0; --i)
            {
                var buff = _buffs[i] as BuffLogic;
                if (buff == null) continue;
                int id = buff.BuffGenInfo.LogicConfigID;
                for (int k = 0; k < logicConfigId.Length; k++)
                {
                    if (logicConfigId[k] == id)
                    {
                        DestroyBuff(buff);
                        _buffs.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// 移除所有完全包含指定BuffTag的buff
        /// </summary>
        public bool RemoveBuffByBuffTag(params int[] buffTag)
        {
            if (buffTag == null || buffTag.Length == 0) return false;
            bool removed = false;
            for (int i = _buffs.Count - 1; i >= 0; --i)
            {
                var buff = _buffs[i] as BuffLogic;
                if (buff == null) continue;
                for (int k = 0; k < buffTag.Length; k++)
                {
                    if (buff.HasBuffTag(buffTag[k]))
                    {
                        DestroyBuff(buff);
                        _buffs.RemoveAt(i);
                        removed = true;
                        break;
                    }
                }
            }

            return removed;
        }

        /// <summary>
        /// 按index移除buff，内部使用
        /// </summary>
        public bool RemoveBuffByIndex(int index)
        {
            if (index < 0 || index >= _buffs.Count)
                return false;
            var buff = _buffs[index] as BuffLogic;
            DestroyBuff(buff);
            _buffs.RemoveAt(index);
            return true;
        }

        /// <summary>
        /// 移除身上全部 Buff，含待创建队列。
        /// </summary>
        public void RemoveAllBuffs()
        {
            for (int i = _buffs.Count - 1; i >= 0; --i)
            {
                var buff = _buffs[i] as BuffLogic;
                DestroyBuff(buff);
                _buffs.RemoveAt(i);
            }

            RecycleCreatingList();
        }

        private void DestroyBuff(BuffLogic buff)
        {
            buff.OnBuffPreviousRemove();
            G.CustomLogic.DestroyLogic(buff);
        }

        // 尚未 CreateLogic 的 GenInfo 由本组件回收，避免失败路径泄漏
        private static void RecycleUnusedGenInfo(BuffGenInfo genInfo)
        {
            G.CustomLogic.DestroyGenInfo(genInfo);
        }

        // 回收待创建队列中尚未落地的 GenInfo
        private void RecycleCreatingList()
        {
            for (int i = 0; i < _creatingList.Count; i++)
            {
                RecycleUnusedGenInfo(_creatingList[i]);
            }
            _creatingList.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This: 驱动

        /// <summary>
        /// 逐帧驱动：先落地待创建 Buff，再更新所有 Buff 并回收已结束的，结束节点可能产生的新 Buff 最后再落地一趟。
        /// </summary>
        public void Update(float dt)
        {
            _updateDepth++;
            try
            {
                FlushPendingBuffs();

                for (int i = _buffs.Count - 1; i >= 0; --i)
                {
                    var buff = _buffs[i] as BuffLogic;

                    buff.UpdateBuff(dt);

                    if (buff.IsFinished())
                    {
                        DestroyBuff(buff);
                        _buffs.RemoveAt(i);
                    }
                }

                FlushPendingBuffs();
            }
            finally
            {
                _updateDepth--;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEntityCommandHandler:

        public bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            foreach (var buff in _buffs)
            {
                if (buff is IEntityCommandHandler commandHandler)
                {
                    if (commandHandler.HandleEntityCommand(entity, cmd))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        public override void DisposeOnRemove()
        {
            for (int i = 0; i < _buffs.Count; ++i)
            {
                var buff = _buffs[i] as BuffLogic;
                buff.OnBuffPreviousRemove();
                G.CustomLogic.DestroyLogic(buff);
            }
            RecycleCreatingList();
            _buffs.Clear();
            _updateDepth = 0;
            base.DisposeOnRemove();
        }
    }


    public partial class LogicEntity
    {
        public BuffCenterComponent comBuffCenter
        {
            get { return (BuffCenterComponent)GetComponent(LogicComponentsLookup.ComBuffCenter); }
        }

        public bool hasComBuffCenter
        {
            get { return HasComponent(LogicComponentsLookup.ComBuffCenter); }
        }

        public void RemoveComBuffCenter()
        {
            RemoveComponent(LogicComponentsLookup.ComBuffCenter);
        }

        public bool HasBuff(int buffLogicId)
        {
            if (!hasComBuffCenter)
            {
                return false;
            }
            return comBuffCenter.HasBuff(buffLogicId);
        }
    }

    public static class LogicEntityExtend_BuffCenter
    {
        static public void SetBuffCenter(this LogicEntity e)
        {
            if (e.hasComBuffCenter)
            {
                G.LogError("SetBuffCenter Error, has buffCenter already");
                return;
            }

            var index = LogicComponentsLookup.ComBuffCenter;
            var component = (BuffCenterComponent)e.CreateComponent(index, typeof(BuffCenterComponent));
            e.AddComponent(index, component);
        }

        static public void AddBuff(this LogicEntity e, BuffGenInfo genInfo)
        {
            if (!e.hasComBuffCenter)
            {
                CLogger.LogDebug("AddBuff unusual, cant find buffCenter");
                e.SetBuffCenter();
            }

            e.comBuffCenter.AddNewBuff(genInfo);
        }

        static public void RemoveBuffByID(this LogicEntity e, int logicConfigId)
        {
            if (!e.hasComBuffCenter)
                return;
            e.comBuffCenter.RemoveBuffByID(logicConfigId);
        }
    }


    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComBuffCenterIndex = new(typeof(BuffCenterComponent));
        public static int ComBuffCenter => _ComBuffCenterIndex.Index;
    }
}
