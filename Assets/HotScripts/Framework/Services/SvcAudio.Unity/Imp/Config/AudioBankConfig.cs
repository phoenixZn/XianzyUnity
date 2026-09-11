using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xease.Audio
{
    [Serializable]
    [CreateAssetMenu(menuName = "KAudio/Audio Bank", fileName = "New Audio Bank.kab")]
    public class AudioBankConfig : AudioConfigBase
    {
        public string Name;
        public List<AudioEventConfig> AudioEvents;
    }
}