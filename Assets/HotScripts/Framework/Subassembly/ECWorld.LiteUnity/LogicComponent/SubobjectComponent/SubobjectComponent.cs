namespace Xease.CoreGame
{
    /// <summary>
    /// 实体子物体：记录来源快照，可选地持有自定义 Logic 与一条关联的批处理记录（基准行为）。
    /// 由 UniSubobjectsComponent.SpawnEntity 挂上；卸载时由 SysSubobject 回收关联记录，本组件销毁 Logic，实体销毁走 ComDeath。
    /// 本组件也是「是否为子物体」的判据，SubobjectSource.Capture 据此沿用父级的归属根。
    /// </summary>
    public class SubobjectComponent : LogicComponent
    {
        public SubobjectSource Source { get; private set; }         // 出生时的来源快照，生命周期内不变
        public SubobjectOrigin Origin { get; private set; }         // 来源的主线程侧引用
        public SubobjectLogic Logic { get; private set; }           // 自定义逻辑；为 null 时只有基准行为
        public SubobjectHandle RoutineHandle { get; private set; }  // 关联记录；无效表示未关联或已断开

        /// <summary>
        /// 写入来源；Logic 与关联记录由 UniSubobjectsComponent 随后设置。
        /// </summary>
        public void Init(in SubobjectSource source, in SubobjectOrigin origin)
        {
            Source = source;
            Origin = origin;
        }

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        public override void DisposeOnRemove()
        {
            RoutineHandle = default;

            if (Logic != null)
            {
                G.CustomLogic.DestroyLogic(Logic);
                Logic = null;
            }
            Source = default;
            Origin = default;
            base.DisposeOnRemove();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 记录仓库分配的关联句柄；移除时由 SysSubobject 负责回收
        internal void LinkRoutine(SubobjectHandle handle)
        {
            RoutineHandle = handle;
        }

        internal void SetLogic(SubobjectLogic logic)
        {
            Logic = logic;
        }

        // 记录被回收或断开时调用；handle 与当前关联不一致时忽略
        internal void ClearRoutineLink(SubobjectHandle handle)
        {
            if (RoutineHandle != handle)
                return;
            RoutineHandle = default;
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public SubobjectComponent comSubobject
        {
            get { return (SubobjectComponent)GetComponent(LogicComponentsLookup.ComSubobject); }
        }

        public bool hasComSubobject
        {
            get { return HasComponent(LogicComponentsLookup.ComSubobject); }
        }

        /// <summary>
        /// 挂上子物体组件；已存在时报错并保留原组件。一般由 UniSubobjectsComponent.SpawnEntity 调用。
        /// </summary>
        public void AddComSubobject(in SubobjectSource source, in SubobjectOrigin origin)
        {
            if (hasComSubobject)
            {
                G.LogError("AddComSubobject 实体已带 SubobjectComponent");
                return;
            }
            var index = LogicComponentsLookup.ComSubobject;
            var component = (SubobjectComponent)CreateComponent(index, typeof(SubobjectComponent));
            component.Init(in source, in origin);
            AddComponent(index, component);
        }

        public void RemoveComSubobject()
        {
            if (hasComSubobject)
            {
                RemoveComponent(LogicComponentsLookup.ComSubobject);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComSubobjectIndex = new(typeof(SubobjectComponent));
        public static int ComSubobject => _ComSubobjectIndex.Index;
    }
}
