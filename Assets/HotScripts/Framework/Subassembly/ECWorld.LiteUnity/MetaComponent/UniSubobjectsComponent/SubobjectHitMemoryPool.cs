using System;

namespace Xease.CoreGame
{
    // 同目标记忆的一项，挂在某条批处理记录的单链表上
    public struct SubobjectHitMemoryEntry
    {
        public int TargetCreationIndex; // 目标的 creationIndex
        public int Count;               // 对该目标已命中的次数
        public float NextTime;          // 对该目标下次允许命中的世界时间戳，秒
        public int Next;                // 链表下一项；-1 表示结尾
    }

    /// <summary>
    /// 同目标命中记忆的共享池。每条批处理记录只保存链表头，表项在一个连续数组里分配与回收，
    /// 目标数量不设上限，也不需要逐帧倒计时。由 UniSubobjectsComponent 持有，仅主线程使用。
    /// </summary>
    public sealed class SubobjectHitMemoryPool
    {
        // 首次分配的表项数，不足时倍增
        private const int InitialCapacity = 64;

        private SubobjectHitMemoryEntry[] _entries = new SubobjectHitMemoryEntry[InitialCapacity];
        private int _freeHead = -1; // 空闲链表头；-1 表示从 _highWater 处取新表项
        private int _highWater;     // 从未分配过的首个下标

        /// <summary>
        /// 在以 head 开头的链表中查找目标，返回表项下标；不存在时返回 -1。
        /// </summary>
        public int Find(int head, int targetCreationIndex)
        {
            for (var i = head; i >= 0; i = _entries[i].Next)
            {
                if (_entries[i].TargetCreationIndex == targetCreationIndex)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// 取表项引用。下标来自 Find 或 Push；在下一次 Push 之前有效。
        /// </summary>
        public ref SubobjectHitMemoryEntry At(int index)
        {
            return ref _entries[index];
        }

        /// <summary>
        /// 为目标新建一项（已命中一次）并插到链表头，返回新的表头。
        /// </summary>
        public int Push(int head, int targetCreationIndex, float nextTime)
        {
            var index = Alloc();
            ref var entry = ref _entries[index];
            entry.TargetCreationIndex = targetCreationIndex;
            entry.Count = 1;
            entry.NextTime = nextTime;
            entry.Next = head;
            return index;
        }

        /// <summary>
        /// 归还以 head 开头的整条链表。
        /// </summary>
        public void FreeChain(int head)
        {
            var i = head;
            while (i >= 0)
            {
                var next = _entries[i].Next;
                _entries[i] = default;
                _entries[i].Next = _freeHead;
                _freeHead = i;
                i = next;
            }
        }

        /// <summary>
        /// 丢弃全部表项，保留已分配的数组。
        /// </summary>
        public void Clear()
        {
            Array.Clear(_entries, 0, _highWater);
            _freeHead = -1;
            _highWater = 0;
        }

        private int Alloc()
        {
            if (_freeHead >= 0)
            {
                var reused = _freeHead;
                _freeHead = _entries[reused].Next;
                return reused;
            }

            if (_highWater == _entries.Length)
                Array.Resize(ref _entries, _entries.Length * 2);
            return _highWater++;
        }
    }
}
