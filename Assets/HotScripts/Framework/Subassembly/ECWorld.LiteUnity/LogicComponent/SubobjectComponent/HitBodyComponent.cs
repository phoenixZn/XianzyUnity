namespace Xease.CoreGame
{
    /// <summary>
    /// 受击体：让实体可以被子物体命中。只描述目标侧的球形范围与层，与发起碰撞一侧的 ColliderComponent 无关。
    /// SysSubobject 每帧把带本组件与 TransformComponent 的实体抄进目标快照。
    /// </summary>
    public class HitBodyComponent : LogicComponent
    {
        public float Radius { get; private set; } // 受击半径，世界单位
        public uint Layer { get; private set; }   // 层位掩码，与 SubobjectHitPolicy.TargetLayers 按位与

        /// <summary>
        /// 写入受击半径与层。
        /// </summary>
        public void Init(float radius, uint layer)
        {
            Radius = radius;
            Layer = layer;
        }

        //////////////////////////////////////////////////////////////////////////
        /// LogicComponent：override

        public override void DisposeOnRemove()
        {
            Radius = 0f;
            Layer = 0;
            base.DisposeOnRemove();
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public HitBodyComponent comHitBody
        {
            get { return (HitBodyComponent)GetComponent(LogicComponentsLookup.ComHitBody); }
        }

        public bool hasComHitBody
        {
            get { return HasComponent(LogicComponentsLookup.ComHitBody); }
        }

        /// <summary>
        /// 添加或改写受击体。
        /// </summary>
        public void SetComHitBody(float radius, uint layer)
        {
            var index = LogicComponentsLookup.ComHitBody;
            var component = (HitBodyComponent)CreateComponent(index, typeof(HitBodyComponent));
            component.Init(radius, layer);
            ReplaceComponent(index, component);
        }

        public void RemoveComHitBody()
        {
            if (hasComHitBody)
            {
                RemoveComponent(LogicComponentsLookup.ComHitBody);
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComHitBodyIndex = new(typeof(HitBodyComponent));
        public static int ComHitBody => _ComHitBodyIndex.Index;
    }
}
