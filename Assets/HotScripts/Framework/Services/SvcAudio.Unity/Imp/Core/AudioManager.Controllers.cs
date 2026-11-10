using System.Collections.Generic;
using UnityEngine;

namespace Xease.Audio
{
    public partial class AudioManager
    {
        private const int _audioSourceInitCount = 20;
        private readonly List<AudioController> _audioControllers = new();
        private readonly Queue<AudioController> _freeAudioControllers = new();

        private void InitController()
        {
            for (var i = 0; i < _audioSourceInitCount; i++)
            {
                AddController();
            }
        }

        private AudioController AddController()
        {
            var gameObject = new GameObject
            {
                name = "Audio Source"
            };
            gameObject.transform.SetParent(AudioRoot.transform);
            gameObject.AddComponent<AudioSource>();
            gameObject.SetActive(false);
            var audioController = new AudioController(gameObject);
            _audioControllers.Add(audioController);
            return audioController;
        }

        public AudioController GetController(AudioEvent audioEvent, AudioTrack track = null)
        {
            AudioController audioController;
            if (_freeAudioControllers.Count == 0)
            {
                audioController = AddController();
            }
            else
            {
                audioController = _freeAudioControllers.Dequeue();
                _audioControllers.Add(audioController);
            }
            if (track != null)
            {
                audioController.Mixer = track.Mixer;
            }
            audioController.SetEvent(audioEvent);
            return audioController;
        }

        public void RemoveController()
        {
            if (_freeAudioControllers.Count == 0)
            {
                return;
            }
            var controller = _freeAudioControllers.Dequeue();
            controller.Recycle();
            if (IsEditor)
            {
                Object.DestroyImmediate(controller.GameObject);
            }
            else
            {
                Object.Destroy(controller.GameObject);
            }
            
        }

        public void RemoveController(int count)
        {
            if (_freeAudioControllers.Count == 0)
            {
                return;
            }
            for (var i = 0; i < count; i++)
            {
                RemoveController();
            }
        }

        private void UpdateController()
        {
            // 按原顺序更新并原地压缩存活项，避免每帧创建捕获实例的筛选委托。
            var activeCount = 0;
            for (var i = 0; i < _audioControllers.Count; i++)
            {
                var audioController = _audioControllers[i];
                if (audioController.GameObject == null)
                {
                    continue;
                }
                if (!audioController.IsActive)
                {
                    _freeAudioControllers.Enqueue(audioController);
                    continue;
                }
                audioController.Update();
                _audioControllers[activeCount++] = audioController;
            }
            if (activeCount < _audioControllers.Count)
            {
                _audioControllers.RemoveRange(activeCount, _audioControllers.Count - activeCount);
            }
            if (_freeAudioControllers.Count > _audioSourceInitCount)
            {
                RemoveController(_freeAudioControllers.Count + _audioControllers.Count - _audioSourceInitCount);
            }
        }

        private void DisposeController()
        {
            foreach (var controller in _audioControllers)
            {
                controller.Clear();
            }
            _audioControllers.Clear();
            foreach (var controller in _freeAudioControllers)
            {
                controller.Clear();
            }
            _freeAudioControllers.Clear();
        }

    }
}
