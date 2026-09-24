using System.Text.RegularExpressions;
using UnityEngine;

namespace Script.Editor.Extended.Engine.AsssetImporter
{
    [System.Serializable]
    public class ParameterOverride
    {
        
        public string propertyName;
        public PropertyType propertyType;
        public string stringValue;
        public int intValue;
        public float floatValue;
        public bool boolValue;
        public Vector2 vector2Value;
        public Vector4 vector4Value;
    
        public enum PropertyType { String, Int, Float, Bool, Enum, Vector2, Vector4 }
        
        public void Reset()
        {
            propertyName = string.Empty;
            stringValue = string.Empty;
            intValue = 0;
            floatValue = 0f;
            boolValue = false;
            vector2Value = Vector2.zero;
            vector4Value = Vector4.zero;
        }
    }

    [System.Serializable]
    public class ImportRule : ISerializationCallbackReceiver
    {
        public const string TextureSettingsPrefix = "textureSettings";

        public static string[] platformNames = new string[] 
        {
            "DefaultTexturePlatform",
            "Android",
            "iPhone",
            "Standalone",
        };
        
        public string ruleName = "New Rule";
        // 匹配条件
        public string pathFilter = "Assets/.*";
        public string matchTag;
        
        public ImportParameterOverride parameterOverride;
    
        internal Regex pathRegex;
        public void OnBeforeSerialize()
        {
            pathRegex = null;
        }

        public void OnAfterDeserialize()
        {
        }
    }

}