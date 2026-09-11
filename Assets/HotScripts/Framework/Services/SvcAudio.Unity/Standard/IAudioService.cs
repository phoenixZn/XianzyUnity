using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xease.Audio;

namespace Xease
{
    /// <summary>
    /// Unity 音频服务：加载 Bank、创建/播放 Event、控制 Track 静音与立体声定位。
    /// 由 AudioManager 实现；逐帧推进走实现类的 IEnvUpdate，不在本接口暴露。
    /// </summary>
    public interface IAudioService : IService
    {
        /// <summary>
        /// 当前音频元配置；未初始化时为 null。
        /// </summary>
        AudioMetaConfig Meta { get; }

        /// <summary>
        /// 立体声定位策略。
        /// </summary>
        IAudioStereo Stereo { get; }

        /// <summary>
        /// 替换立体声定位实现。
        /// </summary>
        void SetStereo(IAudioStereo stereo);

        /// <summary>
        /// 按 Bank 与事件名创建并立即播放。
        /// </summary>
        void Play(string bankName, string eventName);

        /// <summary>
        /// 在 Combat Bank 下按事件名创建，设置世界坐标后播放。
        /// </summary>
        void Play(string eventName, Vector3 pos);

        /// <summary>
        /// 暂停允许暂停的正在播放控制器（游戏暂停，非应用切后台）。
        /// </summary>
        void Pause();

        /// <summary>
        /// 恢复已暂停的控制器。
        /// </summary>
        void Resume();

        /// <summary>
        /// 销毁允许清理的活跃事件。
        /// </summary>
        void Clean();

        /// <summary>
        /// 按 Bank 与事件名创建事件实例；失败返回 null。
        /// </summary>
        AudioEvent CreateEvent(string bankName, string eventName);

        /// <summary>
        /// 按配置上的 Event 类型名收集当前存活事件。
        /// </summary>
        List<AudioEvent> GetEvents(string eventType);

        /// <summary>
        /// 按运行时 Id 查找事件；不存在返回 null。
        /// </summary>
        AudioEvent GetEventById(int eventId);

        /// <summary>
        /// 销毁并回收指定事件。
        /// </summary>
        void DestroyEvent(AudioEvent audioEvent);

        /// <summary>
        /// 按 Id 销毁事件；不存在则忽略。
        /// </summary>
        void DestroyEvent(int eventId);

        /// <summary>
        /// 销毁全部存活事件。
        /// </summary>
        void DestroyAllEvent();

        /// <summary>
        /// 同步加载并登记 Bank；已加载则忽略。
        /// </summary>
        void LoadBank(string bankName);

        /// <summary>
        /// 异步加载并登记 Bank；已登记则立即回调结束。
        /// </summary>
        IEnumerator LoadBankAsync(string bankName, Action callback = null);

        /// <summary>
        /// 卸载并移除已登记 Bank；未加载则忽略。
        /// </summary>
        void UnloadBank(string bankName);

        /// <summary>
        /// 按 Mixer 路径取 Track；未注册返回 null。
        /// </summary>
        AudioTrack GetTrack(string trackName);

        /// <summary>
        /// 过渡到 Mixer Snapshot。
        /// </summary>
        /// <param name="trackName">Snapshot 名</param>
        /// <param name="time">过渡秒数</param>
        void MixerToSnapshot(string trackName, float time);

        /// <summary>
        /// 静音指定 Track。
        /// </summary>
        void Mute(string trackName);

        /// <summary>
        /// 取消静音指定 Track。
        /// </summary>
        void UnMute(string trackName);

        /// <summary>
        /// 静音全部已注册 Track。
        /// </summary>
        void MuteAll();

        /// <summary>
        /// 取消静音全部已注册 Track。
        /// </summary>
        void UnMuteAll();
    }
}
