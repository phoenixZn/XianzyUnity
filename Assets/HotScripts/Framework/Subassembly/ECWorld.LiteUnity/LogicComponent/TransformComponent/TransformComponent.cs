using UnityEngine;

namespace Xease.CoreGame
{
    public class TransformComponent : LogicComponent
    {
        public Vector3 position { get; private set; }
        public Quaternion rotation { get; private set; }
        public Vector3 scale { get; private set; }

        public Vector3 localFaceDir { get; set; } = Vector3.right;

        public Vector3 FaceDir => rotation * localFaceDir;
        
        
        public void Init(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public void SetPosition(Vector3 newPos)
        {
            if (position != newPos)
            {
                position = newPos;
                _owner.ReplaceComponent(LogicComponentsLookup.ComTransform, this);
            }
        }
        
        public void SetRotation(Quaternion newRot)
        {
            if (rotation != newRot)
            {
                rotation = newRot;
                _owner.ReplaceComponent(LogicComponentsLookup.ComTransform, this);
            }
        }

        public void SetScale(Vector3 newScale)
        {
            if (scale != newScale)
            {
                scale = newScale;
                _owner.ReplaceComponent(LogicComponentsLookup.ComTransform, this);
            }
        }
        
    }

    //////////////////////////////////////////////////////////////////////////
    public partial class LogicEntity
    {
        public TransformComponent comTransform
        {
            get { return (TransformComponent)GetComponent(LogicComponentsLookup.ComTransform); }
        }

        public bool hasComTransform
        {
            get { return HasComponent(LogicComponentsLookup.ComTransform); }
        }

        public void SetComTransform(Vector3 newPosition, Quaternion newRotation, Vector3 newScale)
        {
            var index = LogicComponentsLookup.ComTransform;
            if (!hasComTransform)
            {
                var component = (TransformComponent)CreateComponent(index, typeof(TransformComponent));
                component.Init(newPosition, newRotation, newScale);
                AddComponent(index, component);
            }
            else
            {
                comTransform.Init(newPosition, newRotation, newScale);
                ReplaceComponent(index, comTransform);
            }
        }

        public Vector3 position
        {
            get { return comTransform.position; }
        }

        public Quaternion rotation
        {
            get { return comTransform.rotation; }
        }
        
        public Vector3 scale
        {
            get { return comTransform.scale; }
        }
    }
    
    //////////////////////////////////////////////////////////////////////////
    public static partial class EntityExtension
    {
        public static void SetPosition(this LogicEntity entity, Vector3 pos)
        {
            if (entity == null)
                return;
            if (entity.hasComTransform)
                entity.comTransform.SetPosition(pos);
            else
                entity.SetComTransform(pos, Quaternion.identity, Vector3.one);
        }
        
        public static void SetQuaternion(this LogicEntity entity, Quaternion quaternion)
        {
            if (entity == null)
                return;
            if (entity.hasComTransform)
                entity.comTransform.SetRotation(quaternion);
            else
                entity.SetComTransform(Vector3.zero, quaternion, Vector3.one);
        }
        
        public static void SetDir(this LogicEntity entity, Vector3 dir)
        {
            if (entity == null)
                return;
            if (dir.sqrMagnitude < 1e-10f)
                return;
            if (!entity.hasComTransform)
                entity.SetComTransform(Vector3.zero, Quaternion.identity, Vector3.one);
            var comTransform = entity.comTransform;
            var worldFace = comTransform.rotation * comTransform.localFaceDir;
            comTransform.SetRotation(Quaternion.FromToRotation(worldFace, dir) * comTransform.rotation);
        }
    }
    

    //////////////////////////////////////////////////////////////////////////
    public static partial class LogicComponentsLookup
    {
        private static ComponentTypeIndex _ComTransformIndex = new(typeof(TransformComponent));
        public static int ComTransform => _ComTransformIndex.Index;
    }
    
}