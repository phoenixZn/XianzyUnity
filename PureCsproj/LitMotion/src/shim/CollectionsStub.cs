using System;

namespace Unity.Collections
{
    /// <summary>
    /// CLI 托管 NativeList：供 UpdateRunner 收集完成下标后交给 MotionStorage.RemoveAll。
    /// </summary>
    public struct NativeList<T> : IDisposable where T : unmanaged
    {
        T[] items; // 容量缓冲
        int length; // 有效元素个数

        /// <summary>
        /// 按容量分配托管缓冲。
        /// </summary>
        public NativeList(int capacity, Allocator allocator)
        {
            items = capacity <= 0 ? Array.Empty<T>() : new T[capacity];
            length = 0;
        }

        /// <summary>有效元素个数。</summary>
        public int Length => length;

        /// <summary>按下标读取有效元素。</summary>
        public T this[int index] => items[index];

        /// <summary>追加元素，必要时扩容。</summary>
        public void Add(T value)
        {
            if (items == null || length >= items.Length)
            {
                var next = items == null || items.Length == 0 ? 4 : items.Length * 2;
                Array.Resize(ref items, next);
            }
            items[length++] = value;
        }

        /// <summary>释放托管后备引用。</summary>
        public void Dispose()
        {
            items = null;
            length = 0;
        }
    }

    /// <summary>
    /// CLI 空分配器句柄容器：MotionStorage 构造时取得，Reset 时 Rewind 为空操作。
    /// </summary>
    public struct AllocatorHelper<T> : IDisposable where T : struct
    {
        /// <summary>内嵌分配器实例。</summary>
        public T Allocator;

        /// <summary>忽略 Unity Allocator 参数，返回默认实例。</summary>
        public AllocatorHelper(Allocator allocator)
        {
            Allocator = default;
        }

        /// <summary>空释放。</summary>
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// CLI 空 RewindableAllocator：不向 Unity Allocator.Persistent 申请内存。
    /// </summary>
    public struct RewindableAllocator : IDisposable
    {
        /// <summary>空句柄，仅满足 UnsafeAnimationCurve 构造签名。</summary>
        public AllocatorManager.AllocatorHandle Handle;

        /// <summary>空初始化。</summary>
        public void Initialize(int initialSizeInBytes, bool enableBlockFree)
        {
        }

        /// <summary>空回绕。</summary>
        public void Rewind()
        {
        }

        /// <summary>空释放。</summary>
        public void Dispose()
        {
        }
    }

    /// <summary>
    /// CLI 桩：仅提供 AllocatorHandle 类型名。
    /// </summary>
    public static class AllocatorManager
    {
        /// <summary>空分配器句柄。</summary>
        public struct AllocatorHandle
        {
        }
    }
}
