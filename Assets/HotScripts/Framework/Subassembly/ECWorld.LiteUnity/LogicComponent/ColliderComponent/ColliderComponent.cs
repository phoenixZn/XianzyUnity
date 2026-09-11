
namespace Xease.CoreGame
{
    public interface IRawHitMaker
    {
        //生数据
        RawHit[] RawHits { get; }

        /// <summary>
        /// 设置命中缓冲容量。已有数组且长度足够则复用，不会缩小。
        /// </summary>
        void SetCapacity(int capacity);
        
        /// <summary>
        /// 生成生数据
        /// </summary>
        bool MakeRawHits(LogicEntity ownerEntity, float dt);

        //清理当前帧数据
        void Cleanup();
        
        void OnRecycle();
    }

    /// <summary>
    /// 碰撞接口
    /// </summary>
    public interface IEntityColliderHandler
    {
        IRawHitMaker RawHitMaker { get; }

        void SetRawHitMaker<T>() where T : class, IRawHitMaker, new();

        //生成生数据
        bool CheckRawHits(LogicEntity ownerEntity, float dt);

        //处理生数据
        void HandleRawHits(LogicEntity ownerEntity, RawHit[] rawHits, float dt);

        //清理当前帧数据
        void Cleanup();

        bool IsActiveAsSource(LogicEntity ownerEntity);

        //处理碰撞
        void HandleHitEntity(LogicEntity ownerEntity, LogicEntity hitEntity, RawHit rawHit);

        void OnRecycle();
    }

    public class ColliderComponent : LogicComponent
    {
        public bool isActive { get; set; } = true;
        public IEntityColliderHandler handler { get; set; }

        public override void DisposeOnRemove()
        {
            base.DisposeOnRemove();
            isActive = true;
            if (handler != null)
            {
                handler.OnRecycle();
                handler = null;
            }
        }

        public void SetActive(bool active)
        {
            isActive = active;
        }

    }


    public partial class LogicEntity
    {
        public ColliderComponent comCollider
        {
            get { return (ColliderComponent)GetComponent(LogicComponentsLookup.ComCollider); }
        }

        public bool hasComCollider
        {
            get { return HasComponent(LogicComponentsLookup.ComCollider); }
        }
        
        private void ReplaceComCollider(IEntityColliderHandler handler)
        {
            var index = LogicComponentsLookup.ComCollider;
            var component = (ColliderComponent)CreateComponent(index, typeof(ColliderComponent));
            component.handler = handler;
            ReplaceComponent(index, component);
        }

        public void RemoveComCollider()
        {
            RemoveComponent(LogicComponentsLookup.ComCollider);
        }
    }

    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComColliderIndex = new ComponentTypeIndex(typeof(ColliderComponent));
        public static int ComCollider => _ComColliderIndex.Index;
    }
}
