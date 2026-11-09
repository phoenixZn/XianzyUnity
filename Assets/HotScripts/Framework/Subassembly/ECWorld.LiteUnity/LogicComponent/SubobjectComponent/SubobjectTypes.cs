namespace Xease.CoreGame
{
    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 防复用的实体引用。Entitas 会复用实体对象，creationIndex 变化或实体已禁用即视为原实体已不在。
    /// </summary>
    public struct SubobjectEntityRef
    {
        public LogicEntity Entity;
        public int CreationIndex; // 记录时的 creationIndex，用来识别对象被复用

        /// <summary>
        /// 记下实体当前身份；entity 为 null 时返回空引用。
        /// </summary>
        public static SubobjectEntityRef Of(LogicEntity entity)
        {
            if (entity == null)
                return default;
            return new SubobjectEntityRef { Entity = entity, CreationIndex = entity.creationIndex };
        }

        /// <summary>
        /// 原实体仍在世界中时返回 true 并给出实体；已销毁或对象已被复用时返回 false。
        /// </summary>
        public bool TryGet(out LogicEntity entity)
        {
            entity = Entity;
            if (entity != null && entity.isEnabled && entity.creationIndex == CreationIndex)
                return true;
            entity = null;
            return false;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 来源快照：出生时按值抄写，父级销毁后仍可用于记账与过滤。全部字段为值类型，可整体拷进批处理缓冲。
    /// 由子物体再生成子物体时沿用父级的 Root，战果始终记到最初的单位。
    /// </summary>
    public struct SubobjectSource
    {
        public long RootEntityId;       // 战果归属单位的 LogicEntity.ID
        public int RootCreationIndex;   // 战果归属单位的 creationIndex；世界内不复用，可作记账键
        public long ParentEntityId;     // 直接生成者的 LogicEntity.ID，可能是另一个子物体
        public int ParentCreationIndex; // 直接生成者的 creationIndex
        public long PlayerUID;          // 局内玩家；0 表示无玩家归属
        public EFaction Faction;        // 出生时阵营，命中过滤只读这里
        public int RootUnitTid;         // 归属单位的配置 ID，由调用方传入
        public int SkillTid;            // 产生本子物体的技能配置 ID
        public int SubobjectTid;        // 子物体配置 ID，供 CategoryStatsVolumeSubobject 等分类使用

        /// <summary>
        /// 以 parent 为直接生成者抄写来源。parent 本身是子物体时沿用其 Root、玩家与阵营；否则 parent 即归属根。
        /// parent 为 null 表示无来源（如关卡机关），调用方可在返回后自行填写 Faction。
        /// origin 返回同一来源的主线程侧引用，与返回值成对交给生成接口。
        /// </summary>
        public static SubobjectSource Capture(LogicEntity parent, int skillTid, int subobjectTid, int parentUnitTid, out SubobjectOrigin origin)
        {
            var source = new SubobjectSource { SkillTid = skillTid, SubobjectTid = subobjectTid };
            origin = default;
            if (parent == null)
                return source;

            source.ParentEntityId = parent.ID;
            source.ParentCreationIndex = parent.creationIndex;
            origin.Parent = SubobjectEntityRef.Of(parent);

            if (parent.hasComSubobject)
            {
                var parentCom = parent.comSubobject;
                source.RootEntityId = parentCom.Source.RootEntityId;
                source.RootCreationIndex = parentCom.Source.RootCreationIndex;
                source.RootUnitTid = parentCom.Source.RootUnitTid;
                source.PlayerUID = parentCom.Source.PlayerUID;
                source.Faction = parent.hasComFaction ? parent.Faction : parentCom.Source.Faction;
                origin.Root = parentCom.Origin.Root;
                origin.Player = parentCom.Origin.Player ?? parent.GetPlayerInfo();
                return source;
            }

            source.RootEntityId = parent.ID;
            source.RootCreationIndex = parent.creationIndex;
            source.RootUnitTid = parentUnitTid;
            source.Faction = parent.Faction;
            origin.Root = origin.Parent;
            origin.Player = parent.GetPlayerInfo();
            source.PlayerUID = origin.Player != null ? origin.Player.PlayerUID : 0;
            return source;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 来源的主线程侧引用：归属根、直接父级与局内玩家。与 SubobjectSource 成对保存，不进批处理热数据。
    /// </summary>
    public struct SubobjectOrigin
    {
        public SubobjectEntityRef Root;   // 归属根；根实体销毁后 TryGet 返回 false
        public SubobjectEntityRef Parent; // 直接生成者
        public InGamePlayerInfo Player;   // 允许 null，如关卡机关
    }

    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 实体子物体的生成选项。寿命取 SubobjectSpawnArgs.Motion.LifeLeft：带记录时由记录计时，否则转为 ComLife。
    /// </summary>
    public struct SubobjectEntityOptions
    {
        public int LogicConfigId;     // 自定义逻辑配置 ID；0 表示不创建 Logic
        public string LogicContainer; // 逻辑配置容器名；为空时用 UniSubobjectsComponent.DefaultLogicContainer
        public bool WithRoutine;      // 为 true 时同时生成关联的批处理记录（基准行为）
    }
}
