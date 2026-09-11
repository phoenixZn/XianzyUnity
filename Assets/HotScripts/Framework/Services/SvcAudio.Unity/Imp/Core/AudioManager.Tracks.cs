using System.Collections.Generic;
using UnityEngine.Audio;

namespace Xease.Audio
{
    public partial class AudioManager
    {
        // 全局 Mixer 资源句柄
        private AudioAsset<AudioMixer> _mixer;
        // Mixer 路径 → Track
        public readonly Dictionary<string, AudioTrack> _mixerTracks = new();

        //////////////////////////////////////////////////////////////////////////
        /// IAudioService:
        /// <summary>
        /// 按 Mixer 路径取 Track；未注册返回 null。
        /// </summary>
        public AudioTrack GetTrack(string trackName)
        {
            if (trackName is null)
            {
                return null;
            }
            if (_mixerTracks.TryGetValue(trackName, out var track))
            {
                return track;
            }
            return null;
        }

        /// <summary>
        /// 过渡到 Mixer Snapshot。
        /// </summary>
        public void MixerToSnapshot(string trackName, float time)
        {
            var snapshot = _mixer.Asset.FindSnapshot(trackName);
            if (snapshot != null)
            {
                snapshot.TransitionTo(time);
            }
        }

        /// <summary>
        /// 静音指定 Track。
        /// </summary>
        public void Mute(string trackName)
        {
            GetTrack(trackName)?.SetMute(true);
        }

        /// <summary>
        /// 取消静音指定 Track。
        /// </summary>
        public void UnMute(string trackName)
        {
            GetTrack(trackName)?.SetMute(false);
        }

        /// <summary>
        /// 静音全部已注册 Track。
        /// </summary>
        public void MuteAll()
        {
            foreach (var track in _mixerTracks.Values)
            {
                track.SetMute(true);
            }
        }

        /// <summary>
        /// 取消静音全部已注册 Track。
        /// </summary>
        public void UnMuteAll()
        {
            foreach (var track in _mixerTracks.Values)
            {
                track.SetMute(false);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        private void InitTrack()
        {
            _mixerTracks.Clear();
            _mixer = AudioAsset<AudioMixer>.Load("GlobalMixer");
            if (_mixer.Asset is null)
            {
                Audio.LogError("[Audio] load GlobalMixer failed !!!");
                return;
            }

            RegisterTrack("Master/Music");
            RegisterTrack("Master/UI");
            RegisterTrack("Master/Combat");
            RegisterTrack("Master/Ambient");

            // if (!MusicOn)
            // {
            //     MusicTrack.SetMute(true);
            // }
            //
            // if (!SoundOn)
            // {
            //     UITrack.SetMute(true);
            //     CombatTrack.SetMute(true);
            // }
            //
            // MixerToSnapshot("Normal", 0);
        }

        private void DisposeTrack()
        {
            _mixerTracks.Clear();
            _mixer?.Unload();
            _mixer = null;
        }

        private void RegisterTrack(string path)
        {
            if (_mixer is null)
            {
                Audio.LogError("[Audio] Mixer is null, cannot register track.");
                return;
            }
            if (_mixerTracks.ContainsKey(path))
            {
                Audio.LogWarning($"[Audio] Track '{path}' is already registered.");
                return;
            }
            var track = new AudioTrack(_mixer, path);
            _mixerTracks.Add(path, track);
            track.SetVolume(1f);
        }
    }
}