using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using ResourceType = Script.Editor.Extended.Engine.AsssetImporter.ImportParameterOverride.ResourceType;

namespace Script.Editor.Extended.Engine.AsssetImporter
{
    public class KaboomAssetPostprocessor : AssetPostprocessor
    {
        void OnPreprocessTexture() => ApplyRules<TextureImporter>(ResourceType.Texture);
        void OnPreprocessModel() => ApplyRules<ModelImporter>(ResourceType.Model);
        void OnPreprocessAnimation() => ApplyRules<ModelImporter>(ResourceType.Model);
        void OnPreprocessAudio() => ApplyRules<AudioImporter>(ResourceType.Audio);

        private void OnPreprocessAsset()
        {
            // Debug.Log($"OnPreprocessAsset: {assetImporter.assetPath}");
            if (assetImporter is SpriteAtlasImporter)
            {
                ApplyRules<SpriteAtlasImporter>(ResourceType.SpriteAtlas);
            }
            else if (assetImporter is VideoClipImporter)
            {
                ApplyRules<VideoClipImporter>(ResourceType.Video);
            }
            else if (assetImporter is TrueTypeFontImporter)
            {
                ApplyRules<TrueTypeFontImporter>(ResourceType.Font);
            }
        }

        // private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        // {
        //     Debug.Log($"OnPostprocessAllAssets: importedAssets: {string.Join("\n", importedAssets)}");
        // }


        private void ApplyRules<T>(ResourceType targetType) where T : AssetImporter
        {
            // Debug.Log($"ApplyRules: {targetType}");
            
            LightmapSettings settings;
            if (!KaboomImporterPreferences.instance.enableKaboomImporter)
            {
                return;
            }

            var importer = (T) assetImporter;
            foreach (ResourceImportConfig importConfig in KaboomImporterPreferences.instance.resourceImportConfigs)
            {
                if (importConfig == null)
                    continue;

                foreach (var rule in importConfig.rules)
                {
                    if (rule.parameterOverride.resourceType != targetType) 
                        continue;

                    if (!IsMatchRule(importer.assetPath, rule)) 
                        continue;

                    ApplyParameterOverrides(importer, rule.parameterOverride.overrides);
                    // Debug.Log($"Applied rule '{rule.ruleName}' to {assetPath}");
                }
            }
        }

        bool IsPlatformName(string platformName)
        {
            return Array.IndexOf(ImportRule.platformNames, platformName) != -1;
        }

        private bool SetPlatformValue(string properName, ParameterOverride overrides, object platformSettings)
        {
            Type type = platformSettings.GetType();
            if (type.IsClass)
            {
                PropertyInfo propertyInfo = type.GetProperty(properName);
                if (propertyInfo != null)
                {
                    object value = ConvertParameterValue(overrides, propertyInfo.PropertyType);
                    propertyInfo.SetValue(platformSettings, value);
                }
                else
                {
                    FieldInfo fieldInfo = type.GetField(properName);
                    if (fieldInfo != null)
                    {
                        object value = ConvertParameterValue(overrides, fieldInfo.FieldType);
                        fieldInfo.SetValue(platformSettings, value);
                    }
                    else
                    {
                        Debug.LogWarning($"Property or Field'{properName}' not found on {platformSettings.GetType().Name}");
                        return false;
                    }
                }
            }
            else if (type.IsValueType)
            {
                FieldInfo fieldInfo = type.GetField(properName);
                if (fieldInfo == null)
                {
                    Debug.LogWarning($"Field '{properName}' not found on {platformSettings.GetType().Name}");
                    return false;
                }
                object value = ConvertParameterValue(overrides, fieldInfo.FieldType);
                fieldInfo.SetValue(platformSettings, value);
            }
            return true;
        }
        
        void SetPlatformValue(AssetImporter importer, string platformName, string properName, ParameterOverride overrides)
        {
            if (importer is TextureImporter)
            {
                TextureImporter textureImporter = (TextureImporter) importer;
                TextureImporterPlatformSettings platformSettings = textureImporter.GetPlatformTextureSettings(platformName);
                if (SetPlatformValue(properName, overrides, platformSettings))
                {
                    textureImporter.SetPlatformTextureSettings(platformSettings);
                }
            }
            else if (importer is SpriteAtlasImporter)
            {
                SpriteAtlasImporter spriteAtlasImporter = (SpriteAtlasImporter) importer;
                TextureImporterPlatformSettings platformSettings = spriteAtlasImporter.GetPlatformSettings(platformName);
                if (SetPlatformValue(properName, overrides, platformSettings))
                {
                    spriteAtlasImporter.SetPlatformSettings(platformSettings);
                }
            }
            else if (importer is AudioImporter)
            {
                AudioImporter audioImporter = (AudioImporter) importer;
                AudioImporterSampleSettings platformSettings = audioImporter.GetOverrideSampleSettings(platformName);
                object objectValue = platformSettings;
                if (SetPlatformValue(properName, overrides, objectValue))
                {
                    audioImporter.SetOverrideSampleSettings(platformName, (AudioImporterSampleSettings) objectValue);
                }
            }
            else if (importer is VideoClipImporter)
            {
                VideoClipImporter videoImporter = (VideoClipImporter) importer;
                VideoImporterTargetSettings videoTargetSettings = videoImporter.GetTargetSettings(platformName);
                if (videoTargetSettings == null)
                {
                    videoTargetSettings = new VideoImporterTargetSettings();
                }
                if (SetPlatformValue(properName, overrides, videoTargetSettings))
                {
                    videoImporter.SetTargetSettings(platformName, videoTargetSettings);
                }
            }
            // else if (importer is TrueTypeFontImporter)
            // {
            // }
        }

        bool SetPropertyValueRecursively(Type type,object target, List<string> propertyNameList, int nameIndex, ParameterOverride overrides)
        {
            PropertyInfo property = type.GetProperty(propertyNameList[nameIndex]);
            if (property == null || !property.CanWrite)
            {
                Debug.LogWarning($"Property '{propertyNameList[nameIndex]}' not found or read-only on {type.Name}");
                return false;
            }
            
            if(nameIndex == propertyNameList.Count - 1)
            {
                object value = ConvertParameterValue(overrides, property.PropertyType);
                property.SetValue(target, value);
            }
            else
            {
                Type nextType = property.PropertyType;
                object nextTarget = property.GetValue(target);
                if (SetPropertyValueRecursively(nextType, nextTarget, propertyNameList, nameIndex + 1, overrides))
                {
                    property.SetValue(target, nextTarget);
                }
            }
            
            return true;
        }

        private void ApplyParameterOverrides<T>(T importer, List<ParameterOverride> overrides) where T : AssetImporter
        {
            if (overrides == null || overrides.Count == 0) return;

            Type importerType = importer.GetType();
            TextureImporter textureImporter = importer as TextureImporter;
            TextureImporterSettings textureSettings = null;

            foreach (var param in overrides)
            {
                List<string> propertyNames = new List<string>();
                if (param.propertyName.IndexOf("/") != -1)
                {
                    propertyNames.AddRange(param.propertyName.Split('/'));
                }
                else
                {
                    propertyNames.Add(param.propertyName);
                }

                if (textureImporter != null && propertyNames[0] == ImportRule.TextureSettingsPrefix)
                {
                    Debug.Assert(propertyNames.Count > 1);
                    if (textureSettings == null)
                    {
                        textureSettings = new TextureImporterSettings();
                        textureImporter.ReadTextureSettings(textureSettings);
                    }
                    if (!SetPlatformValue(propertyNames[1], param, textureSettings))
                    {
                        Debug.LogWarning($"Failed to set property '{param.propertyName}' on TextureImporterSettings");
                    }
                    continue;
                }

                if (IsPlatformName(propertyNames[0]))
                {
                    Debug.Assert(propertyNames.Count > 1);
                    SetPlatformValue(importer, propertyNames[0], propertyNames[1], param);
                    continue;
                }
                if (!SetPropertyValueRecursively(importerType, importer, propertyNames, 0, param))
                {
                    Debug.LogWarning($"Failed to set property '{param.propertyName}' on {importerType.Name}");
                }
            }

            if (textureImporter != null && textureSettings != null)
            {
                textureImporter.SetTextureSettings(textureSettings);
            }
        }

        private object ConvertParameterValue(ParameterOverride param, Type targetType)
        {
            try
            {
                if (targetType.IsEnum)
                {
                    return Enum.Parse(targetType, param.stringValue);
                }

                switch (param.propertyType)
                {
                    case ParameterOverride.PropertyType.String:
                        return param.stringValue;
                    case ParameterOverride.PropertyType.Int:
                        if (targetType == typeof(uint))
                        {
                            return (uint)param.intValue;
                        }
                        return param.intValue;
                    case ParameterOverride.PropertyType.Float:
                        return param.floatValue;
                    case ParameterOverride.PropertyType.Bool:
                        return param.boolValue;
                    case ParameterOverride.PropertyType.Enum:
                        return Enum.Parse(targetType, param.stringValue);
                    case ParameterOverride.PropertyType.Vector2:
                        return param.vector2Value;
                    case ParameterOverride.PropertyType.Vector4:
                        return param.vector4Value;
                    default:
                        return null;
                }
            }
            catch
            {
                Debug.LogError($"Value conversion failed for {param.propertyName}");
                return null;
            }
        }

        private bool IsMatchRule(string assetPath, ImportRule rule)
        {
            // 路径匹配
            if (!string.IsNullOrEmpty(rule.pathFilter))
            {
                if (rule.pathRegex == null)
                {
                    rule.pathRegex = new Regex(rule.pathFilter);
                }

                if (!rule.pathRegex.IsMatch(assetPath))
                {
                    // Debug.Log($"Path filter not match {assetPath} regex: {rule.pathRegex}");
                    return false;
                }
            }

            // 标签匹配
            if (!string.IsNullOrEmpty(rule.matchTag))
            {
                GUID guid = AssetDatabase.GUIDFromAssetPath(assetPath);
                string[] assetLabels = AssetDatabase.GetLabels(guid);
                if (Array.FindIndex(assetLabels, (label) => label == rule.matchTag) == -1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}