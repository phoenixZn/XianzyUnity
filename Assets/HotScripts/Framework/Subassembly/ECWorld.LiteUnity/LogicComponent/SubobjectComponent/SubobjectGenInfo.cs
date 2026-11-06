namespace Xease.CoreGame
{
    /// <summary>
    /// 实体子物体的生成信息：创建 SubobjectLogic 的运行时初始数据，走对象池，用毕由工厂回收。
    /// OwnerEntity 为子物体自身；SourceEntity 为仍存活的归属根，根已销毁时为 null，战果请改用 Source。
    /// </summary>
    public class SubobjectGenInfo : CustomLogicGenInfo, IHasLogicWorld, IHasMetaWorld, IHasOwnerEntity, IHasSourceEntity, IHasOwnerPlayerInfo
    {
        public SubobjectSource Source { get; protected set; } // 出生时的来源快照
        public SubobjectOrigin Origin { get; protected set; } // 来源的主线程侧引用

        // 仍存活的直接生成者；已销毁或对象已被复用时为 null
        public LogicEntity ParentEntity
        {
            get { return Origin.Parent.TryGet(out var parent) ? parent : null; }
        }

        //////////////////////////////////////////////////////////////////////////
        /// IHasLogicWorld / IHasMetaWorld / IHasOwnerEntity / IHasSourceEntity / IHasOwnerPlayerInfo:

        public LogicWorld LogicWorld { get; protected set; }
        public MetaWorld MetaWorld { get; protected set; }
        public LogicEntity OwnerEntity { get; protected set; }

        public LogicEntity SourceEntity
        {
            get { return Origin.Root.TryGet(out var root) ? root : null; }
        }

        public InGamePlayerInfo OwnerPlayerInfo
        {
            get { return Origin.Player; }
        }

        //////////////////////////////////////////////////////////////////////////
        /// CustomLogicGenInfo：override

        public override void Destroy()
        {
            LogicWorld = null;
            MetaWorld = null;
            OwnerEntity = null;
            Source = default;
            Origin = default;
            base.Destroy();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        // 从对象池取出并初始化。LogicConfigID / ConfigContainerName 由调用方随后填写
        internal static SubobjectGenInfo New(ICustomLogicService svc, MetaWorld metaWorld, LogicEntity ownerEntity, in SubobjectSource source, in SubobjectOrigin origin)
        {
            var genInfo = svc.NewGenInfo<SubobjectGenInfo>();
            genInfo.LogicWorld = ownerEntity.OwnerWorld;
            genInfo.MetaWorld = metaWorld;
            genInfo.OwnerEntity = ownerEntity;
            genInfo.Source = source;
            genInfo.Origin = origin;
            return genInfo;
        }
    }
}
