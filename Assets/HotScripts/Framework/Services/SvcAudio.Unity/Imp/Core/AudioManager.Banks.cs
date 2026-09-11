using System;
using System.Collections;
using System.Collections.Generic;

namespace Xease.Audio
{
    public partial class AudioManager
    {
        // 已加载 Bank：配置名 → 实例
        private readonly Dictionary<string, AudioBank> _audioBanks = new();

        //////////////////////////////////////////////////////////////////////////
        /// IAudioService:
        /// <summary>
        /// 同步加载并登记 Bank；已加载则忽略。
        /// </summary>
        public void LoadBank(string bankName)
        {
            var bank = AudioBank.Load(bankName);
            if (bank?.Config is null || string.IsNullOrEmpty(bank.Config.Name))
            {
                Audio.LogError($"[Audio] AudioBank \"{bankName}\" load failed.");
                return;
            }
            if (_audioBanks.ContainsKey(bank.Config.Name))
            {
                return;
            }
            _audioBanks.Add(bank.Config.Name, bank);
            Audio.Log($"[Audio] AudioBank \"{bankName}\" loaded.");
        }

        /// <summary>
        /// 异步加载并登记 Bank；已登记则立即回调结束。
        /// </summary>
        public IEnumerator LoadBankAsync(string bankName, Action callback = null)
        {
            if (_audioBanks.ContainsKey(bankName))
            {
                callback?.Invoke();
                yield break;
            }
            var bank = AudioBank.Create(bankName);
            if (bank?.Config is null)
            {
                Audio.LogError($"[Audio] AudioBank \"{bankName}\" load failed.");
                callback?.Invoke();
                yield break;
            }
            _audioBanks.Add(bankName, bank);
            yield return bank.LoadAsync(callback);
            Audio.Log($"[Audio] AudioBank \"{bankName}\" loaded async.");
        }

        /// <summary>
        /// 卸载并移除已登记 Bank；未加载则忽略。
        /// </summary>
        public void UnloadBank(string bankName)
        {
            if (!_audioBanks.ContainsKey(bankName))
            {
                return;
            }
            GetBank(bankName)?.Unload();
            _audioBanks.Remove(bankName);
            Audio.Log($"[Audio] AudioBank \"{bankName}\" unloaded.");;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        private void InitBank()
        {
            if (Meta?.InitBanks is null)
            {
                return;
            }
            foreach (var path in Meta.InitBanks)
            {
                G.LogError(path);
                LoadBank(path);
            }
        }

        private AudioBank GetBank(string bankName)
        {
            if (string.IsNullOrEmpty(bankName))
            {
                return null;
            }
            if (_audioBanks.TryGetValue(bankName, out var bank))
            {
                return bank;
            }
            Audio.LogWarning($"[Audio] AudioBank \"{bankName}\" not found");
            return null;
        }

        private void DisposeBank()
        {
            foreach (var bank in _audioBanks.Values)
            {
                bank.Unload();
            }
            _audioBanks.Clear();
        }
    }
}