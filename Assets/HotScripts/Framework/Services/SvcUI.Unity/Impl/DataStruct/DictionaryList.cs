using System.Collections.Generic;

namespace Xease.UI
{
    /// <summary>
    /// 字典 + 插入序 Key/Value 列表，供 UI 子 View 按序遍历。
    /// </summary>
    public class DictionaryList<TKey, TValue>
    {
        private readonly Dictionary<TKey, TValue> _dataDic = new Dictionary<TKey, TValue>();
        private readonly List<TKey> _dataKeyList = new List<TKey>();
        private readonly List<TValue> _dataValueList = new List<TValue>();

        public int Count => _dataDic.Count;

        public List<TKey> KeyList => _dataKeyList;

        public List<TValue> ValueList => _dataValueList;

        public void Add(TKey key, TValue val)
        {
            _dataDic.Add(key, val);
            _dataKeyList.Add(key);
            _dataValueList.Add(val);
        }

        public void Remove(TKey key)
        {
            if (_dataDic.TryGetValue(key, out var val))
            {
                _dataDic.Remove(key);
                _dataValueList.Remove(val);
            }

            _dataKeyList.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue val)
        {
            return _dataDic.TryGetValue(key, out val);
        }

        public void Clear()
        {
            _dataDic.Clear();
            _dataKeyList.Clear();
            _dataValueList.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return _dataDic.ContainsKey(key);
        }

        int GetIdx(TKey key)
        {
            for (int i = 0; i < _dataKeyList.Count; i++)
            {
                if (_dataKeyList[i].Equals(key))
                    return i;
            }

            return -1;
        }

        public TValue this[TKey key]
        {
            get => _dataDic[key];
            set
            {
                _dataDic[key] = value;
                int idx = GetIdx(key);
                if (idx == -1)
                {
                    G.LogError($"[DictionaryList] key = {key} not found!");
                    return;
                }

                _dataValueList[idx] = value;
            }
        }
    }
}
