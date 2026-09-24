using System.Collections.Generic;
using UnityEngine;

namespace Script.Editor.Extended.Engine.AsssetImporter
{
    [CreateAssetMenu(fileName = "ImportParameterOverride", menuName = "KTools/Import Parameter Override")]
    public class ImportParameterOverride : ScriptableObject
    {
        public enum ResourceType
        {
            Texture,
            Model,
            Audio,
            SpriteAtlas,
            Video,
            Font,
        }

        public ResourceType resourceType = ResourceType.Texture;

        public List<ParameterOverride> overrides = new List<ParameterOverride>();
    }
}