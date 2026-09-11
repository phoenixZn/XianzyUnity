using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Xease.Audio
{
    /// <summary>
    /// Unity 音频服务实现：管理 Bank、Track、Event 与 Controller；由 EnvDriver 逐帧驱动。
    /// </summary>
    public partial class AudioManager : IAudioService, IEnvUpdate
    {
        // 是否走编辑器精简初始化（无 Meta/Bank/Track，根节点不 DontDestroyOnLoad）
        public bool IsEditor { get; private set; }
        // 音频源与 Mixer 宿主；运行时 DontDestroyOnLoad
        public GameObject AudioRoot { get; private set; }

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        /// <summary>
        /// 释放全部事件、控制器、Bank、Track 与根节点。
        /// </summary>
        public void Shutdown()
        {
            DisposeEvent();
            DisposeController();
            DisposeBank();
            DisposeMeta();
            DisposeTrack();
            if (AudioRoot == null)
            {
                return;
            }
            if (IsEditor)
            {
                Object.DestroyImmediate(AudioRoot);
            }
            else
            {
                Object.Destroy(AudioRoot);
            }
            AudioRoot = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEnvUpdate:
        /// <summary>
        /// 推进控制器与事件；不使用帧间隔。
        /// </summary>
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            UpdateController();
            UpdateEvent();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IAudioService:
        /// <summary>
        /// 按 Bank 与事件名创建并立即播放。
        /// </summary>
        public void Play(string bankName, string eventName)
        {
            CreateEvent(bankName, eventName)?.Play();
        }

        /// <summary>
        /// 在 Combat Bank 下按事件名创建，设置世界坐标后播放。
        /// </summary>
        public void Play(string eventName, Vector3 pos)
        {
            var audioEvent = CreateEvent("Combat", eventName);
            if (audioEvent != null)
            {
                audioEvent.Pos = pos;
                audioEvent.Play();
            }
        }

        /// <summary>
        /// 暂停允许暂停的正在播放控制器（游戏暂停）。
        /// </summary>
        public void Pause()
        {
            foreach (var controller in _audioControllers.
                         Where(controller =>
                            controller.AudioEvent != null &&
                            controller.AudioEvent.Config != null &&
                            controller.AudioEvent.Config.CanPause &&
                            controller.IsPlaying))
            {
                controller.Pause();
            }
        }

        /// <summary>
        /// 恢复已暂停的控制器。
        /// </summary>
        public void Resume()
        {
            foreach (var controller in _audioControllers.
                         Where(controller => controller.IsPaused))
            {
                controller.Resume();
            }
        }

        /// <summary>
        /// 销毁允许清理的活跃事件。
        /// </summary>
        public void Clean()
        {
            var audioEvents = _audioEvents.Values.ToList();
            foreach (var audioEvent in audioEvents.
                         Where(audioEvent => audioEvent.Config.CanClean))
            {
                DestroyEvent(audioEvent);
            }
            //MixerToSnapshot("Normal", 0);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>
        /// 创建根节点并初始化子系统；isEditor 为 true 时跳过 Meta/Bank/Track。
        /// </summary>
        public void Init(bool isEditor = false)
        {
            IsEditor = isEditor;
            if (IsEditor)
            {
                AudioRoot = new GameObject("TempAudioRoot");
                AudioRoot.transform.SetParent(null);
                AudioRoot.hideFlags = HideFlags.DontSave | HideFlags.DontUnloadUnusedAsset;
                InitAttr();
                InitController();
                return;
            }
            AudioRoot = new GameObject("AudioRoot");
            Object.DontDestroyOnLoad(AudioRoot);
            InitMeta();
            InitAttr();
            InitController();
            InitBank();
            InitTrack();
        }
    }
}
