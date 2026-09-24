using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using Object = System.Object;

namespace Script.Editor.Extended.Engine.AsssetImporter
{
    [CustomEditor(typeof(ImportParameterOverride))]
    public class ResourceImportConfigInspector : UnityEditor.Editor
    {
        private Vector2 scrollPos;

        private Dictionary<string, Type> allImporterTypes = new Dictionary<string, Type>();

        private Dictionary<string, Type> availableProperties;

        void OnEnable()
        {
            // 收集所有AssetImporter类型
            allImporterTypes.Clear();
            allImporterTypes.Add("Texture", typeof(TextureImporter));
            allImporterTypes.Add("Model", typeof(ModelImporter));
            allImporterTypes.Add("Audio", typeof(AudioImporter));
            allImporterTypes.Add("SpriteAtlas", typeof(SpriteAtlasImporter));
            allImporterTypes.Add("Video", typeof(VideoClipImporter));
            allImporterTypes.Add("Font", typeof(TrueTypeFontImporter));
        }

        public override void OnInspectorGUI()
        {
            ImportParameterOverride parameterOverride = target as ImportParameterOverride;
            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            using (var checkScope = new EditorGUI.ChangeCheckScope())
            {
                DrawRule(parameterOverride);
                if (checkScope.changed)
                {
                    EditorUtility.SetDirty(parameterOverride);
                }
            }

            // if (GUILayout.Button("Add New Rule"))
            // {
            //     parameterOverride.overrides.Add(new ImportRule());
            //     EditorUtility.SetDirty(parameterOverride);
            // }

            // EditorGUILayout.Space(20);

            // if (GUILayout.Button("Apply All Rules Now", GUILayout.Height(30)))
            // {
            //     ApplyToAllAssets();
            // }

            EditorGUILayout.EndScrollView();
        }

        static readonly Func<PropertyInfo, bool> _propertyFilter = property => { return property.CanWrite && property.CanRead && property.GetCustomAttribute<ObsoleteAttribute>() == null; };

        static readonly Func<FieldInfo, bool> _fieldFilter = field => { return field.GetCustomAttribute<ObsoleteAttribute>() == null; };


        static void AddPlatformTextureProperties(Type importerType, Dictionary<string, Type> properties)
        {
            bool hasPublicProperties = false;
            if (importerType.IsClass)
            {
                PropertyInfo[] currentProperties = importerType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(_propertyFilter).OrderBy(p => p.Name).ToArray();
                if (currentProperties.Length > 0)
                {
                    hasPublicProperties = true;
                }
                foreach (var platformName in ImportRule.platformNames)
                {
                    foreach (var propertyInfo in currentProperties)
                    {
                        string propertyName = $"{platformName}/{propertyInfo.Name}";
                        properties.Add(propertyName, propertyInfo.PropertyType);
                    }
                }
            }
            
            if (importerType.IsValueType || !hasPublicProperties)
            {
                var currentProperties = importerType
                    .GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(_fieldFilter)
                    .OrderBy(p => p.Name)
                    .ToArray();
                foreach (var platformName in ImportRule.platformNames)
                {
                    foreach (var fieldInfo in currentProperties)
                    {
                        string propertyName = $"{platformName}/{fieldInfo.Name}";
                        properties.Add(propertyName, fieldInfo.FieldType);
                    }
                }
            }
        }

        static void FindAvailablePropertiesRecursive(Type importerType, string parentName, Dictionary<string, Type> properties)
        {
            IEnumerable<PropertyInfo> currentProperties = importerType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(_propertyFilter);

            foreach (var propertyInfo in currentProperties)
            {
                string currentName = $"{parentName}{propertyInfo.Name}";
                if (propertyInfo.PropertyType == typeof(SpriteAtlasTextureSettings) ||
                    propertyInfo.PropertyType == typeof(SpriteAtlasPackingSettings))
                {
                    FindAvailablePropertiesRecursive(propertyInfo.PropertyType, $"{currentName}/", properties);
                }
                else
                {
                    properties.Add(currentName, propertyInfo.PropertyType);
                }
            }
        }

        static string GetPropertyLeafName(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName))
            {
                return propertyName;
            }

            int slashIndex = propertyName.LastIndexOf('/');
            return slashIndex >= 0 ? propertyName.Substring(slashIndex + 1) : propertyName;
        }

        static Dictionary<string, Type> FindAvailableProperties(Type importerType)
        {
            Dictionary<string, Type> properties = new Dictionary<string, Type>();
            FindAvailablePropertiesRecursive(importerType, "", properties);
            if (importerType == typeof(SpriteAtlasImporter) || importerType == typeof(TextureImporter))
            {
                AddPlatformTextureProperties(typeof(TextureImporterPlatformSettings), properties);
            }
            else if (importerType == typeof(AudioImporter))
            {
                AddPlatformTextureProperties(typeof(AudioImporterSampleSettings), properties);
            }
            else if (importerType == typeof(VideoClipImporter))
            {
                AddPlatformTextureProperties(typeof(VideoImporterTargetSettings), properties);
            }

            if (importerType == typeof(TextureImporter))
            {
                FindAvailablePropertiesRecursive(typeof(TextureImporterSettings), $"{ImportRule.TextureSettingsPrefix}/", properties);
            }

            return properties;
        }

        private void DrawRule(ImportParameterOverride parameterOverride)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.Space(2);

            // EditorGUILayout.BeginHorizontal();
            // rule.foldout = EditorGUILayout.Foldout(rule.foldout, $"{index}      {rule.ruleName}");
            // if (GUILayout.Button("X", GUILayout.Width(30)))
            // {
            //     config.rules.RemoveAt(index);
            //     EditorUtility.SetDirty(config);
            //     return;
            // }
            //
            // EditorGUILayout.EndHorizontal();

            {
                // rule.ruleName = EditorGUILayout.TextField("Rule Name", rule.ruleName);
                //
                // rule.pathFilter = EditorGUILayout.TextField("Path Filter", rule.pathFilter);
                ImportParameterOverride.ResourceType resourceType = (ImportParameterOverride.ResourceType) EditorGUILayout.EnumPopup("Resource Type", parameterOverride.resourceType);
                if (resourceType != parameterOverride.resourceType)
                {
                    parameterOverride.overrides.Clear();
                    parameterOverride.resourceType = resourceType;
                }
                //
                // rule.parameterOverride = EditorGUILayout.ObjectField("Parameter Override", rule.parameterOverride, typeof(ImportParameterOverride), false) as ImportParameterOverride;

                // 获取资源类型对应的AssetImporter类型
                string typeName = parameterOverride.resourceType.ToString();
                if (allImporterTypes.TryGetValue(typeName, out var importerType))
                {
                    availableProperties = FindAvailableProperties(importerType);
                }
                else
                {
                    availableProperties = new Dictionary<string, Type>();
                }

                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("Parameter Overrides", EditorStyles.boldLabel);

                // 参数覆盖列表
                EditorGUI.indentLevel++;
                for (int i = 0; i < parameterOverride.overrides.Count; i++)
                {
                    DrawParameterOverride(parameterOverride.overrides[i], i, parameterOverride);
                }

                EditorGUI.indentLevel--;

                EditorGUILayout.Space(10);

                if (GUILayout.Button("Add Parameter Override"))
                {
                    parameterOverride.overrides.Add(new ParameterOverride());
                    EditorUtility.SetDirty(parameterOverride);
                }

                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawParameterOverride(ParameterOverride param, int index, ImportParameterOverride parameterOverride)
        {
            EditorGUILayout.BeginHorizontal();
            if (availableProperties.Count > 0)
            {
                // 属性名称下拉菜单
                List<string> porperlist = availableProperties.Keys.ToList();
                porperlist.Sort((s0, s1) =>
                {
                    if (s0.IndexOf('/') >= 0 && s1.IndexOf('/') < 0)
                    {
                        return -1;
                    }
                    else if (s1.IndexOf('/') >= 0 && s0.IndexOf('/') < 0)
                    {
                        return 1;
                    }
                    else
                    {
                        return s0.CompareTo(s1);
                    }
                });
                
                string[] propertyNames = porperlist.ToArray();
                int selectedIndex = Math.Max(0, Array.FindIndex(propertyNames, s => s == param.propertyName));
                int newSelectedIndex = EditorGUILayout.Popup(selectedIndex, propertyNames);
                if (newSelectedIndex != selectedIndex)
                {
                    param.Reset();
                }

                param.propertyName = propertyNames[newSelectedIndex];
            }
            else
            {
                param.propertyName = EditorGUILayout.TextField("Property", param.propertyName);
            }

            // 根据属性类型显示不同的编辑器字段
            if (!string.IsNullOrEmpty(param.propertyName))
            {
                if (availableProperties.TryGetValue(param.propertyName, out var propertyType))
                {
                    if (propertyType.IsEnum)
                    {
                        param.propertyType = ParameterOverride.PropertyType.Enum;

                        Object enumValue = Enum.GetValues(propertyType).GetValue(0);
                        if (!string.IsNullOrEmpty(param.stringValue))
                        {
                            enumValue = Enum.Parse(propertyType, param.stringValue);
                            // Debug.Log($"parse enumValue: {enumValue} stringValue: {param.stringValue}");
                        }

                        enumValue = EditorGUILayout.EnumPopup((Enum) enumValue);
                        param.stringValue = Enum.GetName(propertyType, enumValue);
                    }
                    else if (propertyType == typeof(string))
                    {
                        param.propertyType = ParameterOverride.PropertyType.String;
                        param.stringValue = EditorGUILayout.TextField(param.stringValue);
                    }
                    else if (propertyType == typeof(int) || propertyType == typeof(uint))
                    {
                        param.propertyType = ParameterOverride.PropertyType.Int;
                        string leafName = GetPropertyLeafName(param.propertyName);
                        if (leafName == "spriteMode")
                        {
                            var mode = (SpriteImportMode)param.intValue;
                            mode = (SpriteImportMode)EditorGUILayout.EnumPopup(mode);
                            param.intValue = (int)mode;
                        }
                        else if (leafName == "spriteAlignment")
                        {
                            var alignment = (SpriteAlignment)param.intValue;
                            alignment = (SpriteAlignment)EditorGUILayout.EnumPopup(alignment);
                            param.intValue = (int)alignment;
                        }
                        else
                        {
                            param.intValue = EditorGUILayout.IntField(param.intValue);
                        }
                    }
                    else if (propertyType == typeof(float))
                    {
                        param.propertyType = ParameterOverride.PropertyType.Float;
                        param.floatValue = EditorGUILayout.FloatField(param.floatValue);
                    }
                    else if (propertyType == typeof(bool))
                    {
                        param.propertyType = ParameterOverride.PropertyType.Bool;
                        param.boolValue = EditorGUILayout.Toggle(param.boolValue);
                    }
                    else if (propertyType == typeof(Vector2))
                    {
                        param.propertyType = ParameterOverride.PropertyType.Vector2;
                        param.vector2Value = EditorGUILayout.Vector2Field(GUIContent.none, param.vector2Value);
                    }
                    else if (propertyType == typeof(Vector4))
                    {
                        param.propertyType = ParameterOverride.PropertyType.Vector4;
                        param.vector4Value = EditorGUILayout.Vector4Field(GUIContent.none, param.vector4Value);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"Unsupported type: {propertyType.Name}", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox($"Property not found: {param.propertyName}", MessageType.Warning);
                }
            }

            if (GUILayout.Button("X", GUILayout.Width(30)))
            {
                parameterOverride.overrides.RemoveAt(index);
                EditorUtility.SetDirty(parameterOverride);
                return;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}