using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 单一语言的字符串表：按 key 存取原文。
    /// </summary>
    internal partial class LocalizationData
    {
        private Language _language = Language.unknown;

        // en 约 4300 条，向上取整
        private readonly Dictionary<string, string> _dictionary = new(5000);

        /// <summary>
        /// 取未格式化的原文；key 为空或不存在时返回 null。
        /// </summary>
        public string GetRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (!_dictionary.TryGetValue(key, out var value))
            {
                return null;
            }

            return value;
        }

        /// <summary>
        /// 当前表是否包含该 key。
        /// </summary>
        public bool HasRawString(string key) => _dictionary.ContainsKey(key);

        /// <summary>
        /// 批量写入原文。key 已存在时跳过并打错误日志。
        /// </summary>
        public void AddRawString(Dictionary<string, string> pairs)
        {
            if (pairs == null)
                return;

            foreach (var pair in pairs)
            {
                AddRawString(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// 写入一条原文。key 非法或重复时打错误日志并跳过。
        /// </summary>
        public void AddRawString(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
            {
                G.LogError("[Localization] AddRawString Failed: invalid key");
                return;
            }

            if (_dictionary.ContainsKey(key))
            {
                G.LogError($"[Localization] AddRawString Failed: duplicated key: {key}");
                return;
            }

            _dictionary.Add(key, value ?? string.Empty);
        }

        /// <summary>
        /// 移除一条原文。key 为空时忽略。
        /// </summary>
        public void RemoveRawString(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            _dictionary.Remove(key);
        }
    }
}
