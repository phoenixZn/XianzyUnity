using System;
using System.Collections.Generic;

namespace Xease.CoreGame
{
    //////////////////////////////////////////////////////////////////////////
    /// <summary>
    /// 单值类型属性修改器表：key 在 [0, FastKeyCapacity) 走定长数组，其余走冷字典；与将来 int 脏标记位宽对齐。
    /// </summary>
    public class AttributeModifiers<TValue> : IAttributes
    {
        // 热 key 容量；与 int 脏标记 32 位对齐，改容量需同步脏标记方案
        public const int FastKeyCapacity = 32;

        // 热槽；下标即 attrName，null 表示未占用；
        private readonly IModifyValue<TValue>[] _fastSlots = new IModifyValue<TValue>[FastKeyCapacity];
        // 冷 key 字典；仅 key 越界时使用；
        private readonly Dictionary<int, IModifyValue<TValue>> _coldDict = new();

        //////////////////////////////////////////////////////////////////////////
        /// IAttributes:
        public System.Type AttributeType
        {
            get { return typeof(TValue); }
        }

        /// <summary>注销全部属性条目；保留热槽数组与冷字典实例。</summary>
        public void Clear()
        {
            Array.Clear(_fastSlots, 0, _fastSlots.Length);
            _coldDict.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>是否已注册指定 attrName 的修改器。</summary>
        public bool ContainsKey(int key)
        {
            if ((uint)key < FastKeyCapacity)
            {
                return _fastSlots[key] != null;
            }
            return _coldDict.ContainsKey(key);
        }

        /// <summary>按 attrName 取修改器；未注册时返回 false。</summary>
        public bool TryGetValue(int key, out IModifyValue<TValue> value)
        {
            if ((uint)key < FastKeyCapacity)
            {
                value = _fastSlots[key];
                return value != null;
            }
            return _coldDict.TryGetValue(key, out value);
        }

        /// <summary>注册修改器；同 key 已存在时抛 ArgumentException（对齐 Dictionary.Add）。</summary>
        public void Add(int key, IModifyValue<TValue> value)
        {
            if ((uint)key < FastKeyCapacity)
            {
                if (_fastSlots[key] != null)
                {
                    throw new ArgumentException("An item with the same key has already been added.");
                }
                _fastSlots[key] = value;
                return;
            }
            _coldDict.Add(key, value);
        }
    }
}