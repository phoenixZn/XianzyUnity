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
            if (_freeAudioControllers.Count == 0)
            {
                return AddController();
            }
            var audioController = _freeAudioControllers.Dequeue();
            _audioControllers.Add(audioController);
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
            _audioControllers.RemoveAll(audioController =>
            {
                if (audioController.GameObject is null)
                {
                    return true;
                }
                if (audioController.IsActive)
                {
                    audioController.Update();
                    return false;
                }
                _freeAudioControllers.Enqueue(audioController);
                return true;
            });
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