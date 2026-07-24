using Entitas;

namespace Xease.CoreGame
{
    public class IDComponent : LogicComponent
    {
        public long ID { get; private set; }
        public string Name { get; private set; }

        public void Init(long id, string name)
        {
            ID = id;
            Name = name;
        }
        
        public void SetID(long id)
        {
            if (ID == id)
                return;

            // 无宿主时仅改本地字段；有宿主则新实例 Replace，保留 previous 旧 ID 供索引 remove
            if (_hostEntity == null)
            {
                ID = id;
                return;
            }

            var index = LogicComponentsLookup.ComID;
            var next = (IDComponent)_hostEntity.CreateComponent(index, typeof(IDComponent));
            next.Init(id, Name);
            _hostEntity.ReplaceComponent(index, next);
        }
        
        public void SetName(string name)
        {
            if (Name == name)
                return;

            // 无宿主时仅改本地字段；有宿主则新实例 Replace，保留 previous 旧 Name 供索引 remove
            if (_hostEntity == null)
            {
                Name = name;
                return;
            }

            var index = LogicComponentsLookup.ComID;
            var next = (IDComponent)_hostEntity.CreateComponent(index, typeof(IDComponent));
            next.Init(ID, name);
            _hostEntity.ReplaceComponent(index, next);
        }
        
        public override void DisposeOnRemove()
        {
            Name = null;
            ID = 0;
            base.DisposeOnRemove();
        }
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public IDComponent comID { get { return (IDComponent)GetComponent(LogicComponentsLookup.ComID); } }
        public bool hasComID { get { return HasComponent(LogicComponentsLookup.ComID); } }
        
        public void AddComID(long id, string name = null)
        {
            var index = LogicComponentsLookup.ComID;
            if (index < 0)
            {
                WLogger.LogError("AddComID 未初始化的组件索引 LogicComponentsLookup.ComID");
                return;
            }
            var component = (IDComponent)CreateComponent(index, typeof(IDComponent));
            component.Init(id, name);
            AddComponent(index, component);
        }
        
        public long ID
        {
            get
            {
                if (hasComID)
                {
                    return comID.ID;
                }
                return creationIndex;
            }
        } 
    }
    

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComIDIndex = new (typeof(IDComponent));
        public static int ComID => _ComIDIndex.Index;
    }
    
}