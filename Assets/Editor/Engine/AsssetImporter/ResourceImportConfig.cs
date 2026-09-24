using System.Collections.Generic;
using UnityEngine;

namespace Script.Editor.Extended.Engine.AsssetImporter
{
    [CreateAssetMenu(fileName = "ResourceImportConfig", menuName = "KTools/Import Config")]
    public class ResourceImportConfig : ScriptableObject
    {
        public List<ImportRule> rules = new List<ImportRule>();
    }
}