using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Script.Editor.Extended.Engine.AsssetImporter
{
    [FilePath("ProjectSettings/KaboomImporterPreferences.asset", FilePathAttribute.Location.ProjectFolder)]
    public class KaboomImporterPreferences : ScriptableSingleton<KaboomImporterPreferences>
    {
        [SerializeField]
        public bool enableKaboomImporter = true;
        
        [SerializeField]
        public List<ResourceImportConfig> resourceImportConfigs = new List<ResourceImportConfig>();
        
        public void Save()
        {
            Save(true);
        }
        void OnDisable()
        {
            Save(true);
        }
        public SerializedObject GetSerializedObject()
        {
            return new SerializedObject(this);
        }
    }

    class KaboomImporterPreferencesProvider : SettingsProvider
    {
        SerializedObject m_SerializedObject;
        SerializedProperty m_enableKaboomImporter;
        SerializedProperty m_resourceImportConfigs;
        internal class Styles
        {
        }

        public KaboomImporterPreferencesProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
        {
        }

        public override void OnActivate(string searchContext, VisualElement rootElement)
        {
            m_SerializedObject = KaboomImporterPreferences.instance.GetSerializedObject();
            m_enableKaboomImporter = m_SerializedObject.FindProperty("enableKaboomImporter");
            m_resourceImportConfigs = m_SerializedObject.FindProperty("resourceImportConfigs");
        }

        public override void OnGUI(string searchContext)
        {
            m_SerializedObject.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space();
            {
                EditorGUILayout.PropertyField(m_enableKaboomImporter);
                EditorGUILayout.PropertyField(m_resourceImportConfigs);
            }
            EditorGUILayout.EndVertical();
            bool valueChanged = EditorGUI.EndChangeCheck();
            if (valueChanged)
            {
                m_SerializedObject.ApplyModifiedProperties();
                KaboomImporterPreferences.instance.Save();
            }
            
        }

        [SettingsProvider]
        public static SettingsProvider CreateTimelineProjectSettingProvider()
        {
            var provider = new KaboomImporterPreferencesProvider("Kaboom/AssetImporter", SettingsScope.Project, GetSearchKeywordsFromGUIContentProperties<Styles>());
            return provider;
        }
    }

}