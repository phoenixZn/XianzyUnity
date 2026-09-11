using System.IO;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Xease.Audio.Editor
{
    /// <summary>
    /// 将 .kab（JSON 文本）导入为 AudioBankConfig，供对象选择器与音频库编辑器使用。
    /// </summary>
    [ScriptedImporter(2, "kab")]
    public class AudioBankConfigImporter : ScriptedImporter
    {
        /// <summary>
        /// 反序列化 JSON 并登记为主资源。
        /// </summary>
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = AudioConfigBase.Deserialize<AudioBankConfig>(File.ReadAllText(ctx.assetPath)) as AudioBankConfig;
            if (asset == null)
            {
                return;
            }
            asset.name = Path.GetFileNameWithoutExtension(ctx.assetPath);
            ctx.AddObjectToAsset("AudioBankConfig", asset);
            ctx.SetMainObject(asset);
        }
    }
}
