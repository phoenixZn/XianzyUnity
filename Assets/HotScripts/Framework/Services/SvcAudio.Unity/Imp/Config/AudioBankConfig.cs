using System;
using System.Collections.Generic;

namespace Xease.Audio
{
    /// <summary>
    /// Bank 配置。.kab 是 JSON 文本：编辑器经 ScriptedImporter 导入，运行时按 RawFile 读取。
    /// </summary>
    [Serializable]
    public class AudioBankConfig : AudioConfigBase
    {
        public string Name;
        public List<AudioEventConfig> AudioEvents;
    }
}