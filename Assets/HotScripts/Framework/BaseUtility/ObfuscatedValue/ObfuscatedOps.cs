using System;
using System.Runtime.CompilerServices;

namespace Xease
{
    /// <summary>
    /// <see cref="Obfuscated{T}"/> 的位运算：按与整数相同的位宽 XOR，不分配、不引用引擎。
    /// </summary>
    internal static class ObfuscatedOps
    {
        // 与 int/uint/float 相同的载荷宽度
        private const int Size32 = 4;
        // 与 long/ulong/double 相同的载荷宽度
        private const int Size64 = 8;

        // 对 a、b 的位模式逐宽异或；float/int 都按底层比特处理
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Xor<T>(T a, T b) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            if (size == Size32)
            {
                uint x = Unsafe.As<T, uint>(ref a) ^ Unsafe.As<T, uint>(ref b);
                return Unsafe.As<uint, T>(ref x);
            }

            if (size == Size64)
            {
                ulong x = Unsafe.As<T, ulong>(ref a) ^ Unsafe.As<T, ulong>(ref b);
                return Unsafe.As<ulong, T>(ref x);
            }

            throw UnsupportedType<T>();
        }

        // 按位模式是否全 0 判断哨兵，不用数值相等（避开 float 的 -0 / NaN）
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool IsZeroBits<T>(T value) where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            if (size == Size32)
            {
                return Unsafe.As<T, uint>(ref value) == 0u;
            }

            if (size == Size64)
            {
                return Unsafe.As<T, ulong>(ref value) == 0ul;
            }

            throw UnsupportedType<T>();
        }

        // 对已知数值类型做明文加法；其它 T 应走 Value 自行运算
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static T Add<T>(T left, T right) where T : unmanaged
        {
            if (typeof(T) == typeof(int))
            {
                int sum = Unsafe.As<T, int>(ref left) + Unsafe.As<T, int>(ref right);
                return Unsafe.As<int, T>(ref sum);
            }

            if (typeof(T) == typeof(uint))
            {
                uint sum = Unsafe.As<T, uint>(ref left) + Unsafe.As<T, uint>(ref right);
                return Unsafe.As<uint, T>(ref sum);
            }

            if (typeof(T) == typeof(long))
            {
                long sum = Unsafe.As<T, long>(ref left) + Unsafe.As<T, long>(ref right);
                return Unsafe.As<long, T>(ref sum);
            }

            if (typeof(T) == typeof(ulong))
            {
                ulong sum = Unsafe.As<T, ulong>(ref left) + Unsafe.As<T, ulong>(ref right);
                return Unsafe.As<ulong, T>(ref sum);
            }

            if (typeof(T) == typeof(float))
            {
                float sum = Unsafe.As<T, float>(ref left) + Unsafe.As<T, float>(ref right);
                return Unsafe.As<float, T>(ref sum);
            }

            if (typeof(T) == typeof(double))
            {
                double sum = Unsafe.As<T, double>(ref left) + Unsafe.As<T, double>(ref right);
                return Unsafe.As<double, T>(ref sum);
            }

            throw new NotSupportedException($"Obfuscated<{typeof(T).Name}>.Add is not supported.");
        }

        // 是否为 XOR 可处理的载荷宽度
        internal static bool IsSupportedSize<T>() where T : unmanaged
        {
            int size = Unsafe.SizeOf<T>();
            return size == Size32 || size == Size64;
        }

        internal static NotSupportedException UnsupportedType<T>()
        {
            return new NotSupportedException(
                $"Obfuscated<{typeof(T).Name}> requires a 4-byte or 8-byte unmanaged payload.");
        }
    }
}
