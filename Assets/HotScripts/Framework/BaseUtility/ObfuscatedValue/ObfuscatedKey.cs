using System;
using System.Runtime.InteropServices;

namespace Xease
{
    /// <summary>
    /// 为 <see cref="Obfuscated{T}"/> 生成非 0 位模式的 XOR key；使用 <see cref="Random"/>，与引擎全局随机隔离。
    /// </summary>
    internal static class ObfuscatedKey
    {
        // 仅用于生成 XOR key，不参与玩法随机
        private static readonly Random s_random = new Random();
        // 保护 s_random；生成只发生在首次写入
        private static readonly object s_lock = new object();

        // 循环直到位模式非全 0，避免与「尚未生成 key」哨兵冲突
        internal static T Create<T>() where T : unmanaged
        {
            T key;
            do
            {
                key = Fill<T>();
            }
            while (ObfuscatedOps.IsZeroBits(key));

            return key;
        }

        // 向栈上的 T 填满随机字节，不分配
        private static T Fill<T>() where T : unmanaged
        {
            T key = default;
            lock (s_lock)
            {
                Span<byte> bytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref key, 1));
                s_random.NextBytes(bytes);
            }

            return key;
        }
    }
}
