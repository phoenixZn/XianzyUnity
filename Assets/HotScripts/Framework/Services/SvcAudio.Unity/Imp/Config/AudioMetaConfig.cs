using System;
using System.Collections.Generic;

namespace Xease.Audio
{
    [Serializable]
    public class AudioMetaConfig
    {
        public static readonly string Path = "AudioMeta";
        
        public List<string> InitBanks;
    }
}