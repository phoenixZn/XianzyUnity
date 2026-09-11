using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Xease.Audio.Editor
{
    /// <summary>
    /// 创建 JSON 格式的 .kab，避免 CreateAssetMenu 把 Bank 写成 Unity YAML。
    /// </summary>
    public static class AudioBankMenu
    {
        /// <summary>
        /// 在当前选中目录生成一份空的音频库。
        /// </summary>
        [MenuItem("Assets/Create/KAudio/Audio Bank")]
        public static void CreateAudioBank()
        {
            var dir = GetSelectedAssetDirectory();
            var path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/NewAudioBank.kab");
            var config = ScriptableObject.CreateInstance<AudioBankConfig>();
            config.Name = Path.GetFileNameWithoutExtension(path);
            config.AudioEvents = new List<AudioEventConfig>();
            config.Save(path);
            AssetDatabase.ImportAsset(path);
            var imported = AssetDatabase.LoadAssetAtPath<AudioBankConfig>(path);
            Selection.activeObject = imported;
            EditorGUIUtility.PingObject(imported);
        }

        private static string GetSelectedAssetDirectory()
        {
            var obj = Selection.activeObject;
            if (obj == null)
            {
                return "Assets";
            }
            var path = AssetDatabase.GetAssetPath(obj);
            if (string.IsNullOrEmpty(path))
            {
                return "Assets";
            }
            if (AssetDatabase.IsValidFolder(path))
            {
                return path;
            }
            var parent = Path.GetDirectoryName(path);
            return string.IsNullOrEmpty(parent) ? "Assets" : parent.Replace('\\', '/');
        }
    }
}
