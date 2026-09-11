using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Xease.Audio
{
    public partial class AudioManager
    {
        // 存活事件：运行时 Id → 实例
        private readonly Dictionary<int, AudioEvent> _audioEvents = new();
        // 按事件名/Tag 统计并发与最小间隔
        private readonly Dictionary<string, AudioEventGroup> _audioEventGroups = new();
        // 下一枚事件 Id 种子；实际 Id 避开已占用键
        private int _audioEventId = 0;

        //////////////////////////////////////////////////////////////////////////
        /// IAudioService:
        /// <summary>
        /// 按 Bank 与事件名创建事件实例；失败返回 null。
        /// </summary>
        public AudioEvent CreateEvent(string bankName, string eventName)
        {
            var bank = GetBank(bankName);
            if (bank is null)
            {
                return null;
            }
            var eventConfig = bank.GetEventConfig(eventName);
            if (eventConfig is null)
            {
                return null;
            }
            var eventType = GetEventType(eventConfig.Event);
            if (eventType is null)
            {
                return null;
            }
            if (!IncEventGroupCounter(eventConfig))
            {
                return null;
            }
            var audioEvent = AudioEventPool.CreateEvent(eventType);
            if (audioEvent is null)
            {
                DecEventGroupCounter(eventConfig);
                return null;
            }
            audioEvent.Init(this, bank, eventConfig);
            _audioEvents.Add(audioEvent.Id, audioEvent);
            audioEvent.Awake();
            return audioEvent;
        }

        /// <summary>
        /// 按配置上的 Event 类型名收集当前存活事件。
        /// </summary>
        public List<AudioEvent> GetEvents(string eventType)
        {
            return _audioEvents.Values.Where(audioEvent => audioEvent.Config.Event == eventType).ToList();
        }

        /// <summary>
        /// 按运行时 Id 查找事件；不存在返回 null。
        /// </summary>
        public AudioEvent GetEventById(int eventId)
        {
            return _audioEvents.TryGetValue(eventId, out var audioEvent) ? audioEvent : null;
        }

        /// <summary>
        /// 销毁并回收指定事件。
        /// </summary>
        public void DestroyEvent(AudioEvent audioEvent)
        {
            if (audioEvent is null)
            {
                return;
            }
            if (!_audioEvents.ContainsKey(audioEvent.Id))
            {
                return;
            }
            DecEventGroupCounter(audioEvent.Config);
            _audioEvents.Remove(audioEvent.Id);
            audioEvent.Destroy();
            AudioEventPool.ReleaseEvent(audioEvent);
        }

        /// <summary>
        /// 按 Id 销毁事件；不存在则忽略。
        /// </summary>
        public void DestroyEvent(int eventId)
        {
            GetEventById(eventId)?.Destroy();
        }

        /// <summary>
        /// 销毁全部存活事件。
        /// </summary>
        public void DestroyAllEvent()
        {
            foreach (var audioEvent in _audioEvents.Values.ToList())
            {
                DestroyEvent(audioEvent);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>
        /// 分配未占用的事件运行时 Id。
        /// </summary>
        public int NewEventId()
        {
            var id = Interlocked.Increment(ref _audioEventId);
            while (_audioEvents.ContainsKey(id))
            {
                id = Interlocked.Increment(ref _audioEventId);
            }
            return id;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器预览：不绑 Bank，直接用给定配置创建事件。
        /// </summary>
        public AudioEvent CreateEvent(AudioEventConfig config)
        {
            var eventType = GetEventType(config.Event);
            if (eventType is null)
            {
                return null;
            }
            var audioEvent = AudioEventPool.CreateEvent(eventType);
            if (audioEvent is null)
            {
                return null;
            }
            audioEvent.Init(this, null, config);
            _audioEvents.Add(audioEvent.Id, audioEvent);
            audioEvent.Awake();
            return audioEvent;
        }
#endif

        // 推进存活事件；失活或异常则销毁
        private void UpdateEvent()
        {
            var audioEventList = _audioEvents.Values.ToList();
            foreach (var audioEvent in audioEventList)
            {
                try
                {
                    audioEvent?.Update();
                    if (audioEvent is null || !audioEvent.IsActive)
                    {
                        DestroyEvent(audioEvent);
                    }
                }
                catch (Exception e)
                {
                    Audio.LogError(e);
                    DestroyEvent(audioEvent);
                }
            }
            foreach (var kv in _audioEventGroups)
            {
                kv.Value.Update();
            }
        }

        private void DisposeEvent()
        {
            var audioEvents = _audioEvents.Values.ToList();
            foreach (var audioEvent in audioEvents)
            {
                DestroyEvent(audioEvent);
            }
        }

        private bool IncEventGroupCounter(AudioEventConfig cfg)
        {
            AudioEventGroup selfGroup = null;
            AudioEventGroup tagGroup = null;
            if (!_audioEventGroups.TryGetValue(cfg.Name, out selfGroup))
            {
                selfGroup = Audio.PoolAllocate<AudioEventGroup>();
            }
            if (!string.IsNullOrEmpty(cfg.TagName))
            {
                if (!_audioEventGroups.TryGetValue(cfg.TagName, out tagGroup))
                {
                    tagGroup = Audio.PoolAllocate<AudioEventGroup>();
                }
            }

            if (cfg.MaxStack > 0 && selfGroup.EventCount >= cfg.MaxStack)
            {
                return false;
            }
            if (tagGroup != null && cfg.MaxStack > 0 && tagGroup.EventCount >= cfg.MaxStack && selfGroup.EventCount > 0)
            {
                return false;
            }
            if (selfGroup.LastEventTime >= 0 && selfGroup.LastEventTime < cfg.MinInterval)
            {
                return false;
            }

            selfGroup.EventCount++;
            selfGroup.LastEventTime = 0;
            _audioEventGroups[cfg.Name] = selfGroup;
            if (tagGroup != null)
            {
                tagGroup.EventCount++;
                tagGroup.LastEventTime = 0;
                _audioEventGroups[cfg.TagName] = tagGroup;
            }
            return true;
        }

        private void DecEventGroupCounter(AudioEventConfig cfg)
        {
            if (cfg is null)
            {
                return;
            }

            AudioEventGroup selfGroup = null;
            AudioEventGroup tagGroup = null;
            _audioEventGroups.TryGetValue(cfg.Name, out selfGroup);
            if (!string.IsNullOrEmpty(cfg.TagName))
            {
                _audioEventGroups.TryGetValue(cfg.TagName, out tagGroup);
            }

            if (selfGroup != null)
            {
                selfGroup.EventCount--;
                if (selfGroup.EventCount > 0)
                {
                    _audioEventGroups[cfg.Name] = selfGroup;
                }
                else
                {
                    _audioEventGroups.Remove(cfg.Name);
                    Audio.PoolRecycle(selfGroup);
                }
            }


            if (tagGroup != null)
            {
                tagGroup.EventCount--;
                if (tagGroup.EventCount > 0)
                {
                    _audioEventGroups[cfg.TagName] = tagGroup;
                }
                else
                {
                    _audioEventGroups.Remove(cfg.TagName);
                    Audio.PoolRecycle(tagGroup);
                }
            }
        }

    }
}