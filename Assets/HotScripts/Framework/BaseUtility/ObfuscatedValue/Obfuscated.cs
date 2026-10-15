using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Xease
{
    /// <summary>
    /// 对非托管数值做 XOR 位混淆，使内存中不直接存储明文。
    /// 轻度防扫描，并非密码学加密。可变 struct，应作为字段存放；经属性取出后再写会改到副本。
    /// 支持 int、uint、float、long、ulong、double。
    /// </summary>
    public struct Obfuscated<T> : IEquatable<Obfuscated<T>> where T : unmanaged, IEquatable<T>
    {
        // 与 _key 异或后的位模式；key 全 0 时即为明文，供 default 实例使用
        private T _hidden;
        // XOR key；位模式全 0 表示尚未生成，此时读写明文恒等
        private T _key;

        static Obfuscated()
        {
            // 不支持的 T 在类型初始化时失败，避免热路径反复检查
            if (!ObfuscatedOps.IsSupportedSize<T>())
            {
                throw ObfuscatedOps.UnsupportedType<T>();
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This:

        /// <summary>
        /// 使用随机非 0 key 混淆指定明文。
        /// </summary>
        /// <param name="value">要混淆的原始值。</param>
        public Obfuscated(T value)
        {
            _hidden = default;
            _key = default;
            SetPlain(value);
        }

        /// <summary>
        /// 解密后的当前值。写入时若尚未生成 key 则创建随机非 0 key。
        /// get 不改字段。
        /// </summary>
        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetPlain();
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => SetPlain(value);
        }

        /// <summary>
        /// 将当前值设为指定明文。
        /// </summary>
        /// <param name="value">新的原始值。</param>
        public void SetValue(T value)
        {
            SetPlain(value);
        }

        /// <summary>
        /// 将当前明文加上增量后再写回。支持 int、uint、long、ulong、float、double。
        /// </summary>
        /// <param name="delta">增量。</param>
        public void Add(T delta)
        {
            SetPlain(ObfuscatedOps.Add(GetPlain(), delta));
        }

        /// <summary>
        /// 隐式转换为解密后的明文。
        /// </summary>
        public static implicit operator T(Obfuscated<T> value)
        {
            return value.GetPlain();
        }

        // key 全 0 时 XOR 恒等，default 读出 default(T)；get 路径不生成 key
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private T GetPlain()
        {
            return ObfuscatedOps.Xor(_hidden, _key);
        }

        // 首次写入时生成非 0 key，再混淆明文
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetPlain(T value)
        {
            if (ObfuscatedOps.IsZeroBits(_key))
            {
                _key = ObfuscatedKey.Create<T>();
            }

            _hidden = ObfuscatedOps.Xor(value, _key);
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEquatable:

        /// <summary>
        /// 基于解密后的明文判断是否相等。
        /// </summary>
        public bool Equals(Obfuscated<T> other)
        {
            return GetPlain().Equals(other.GetPlain());
        }

        /// <summary>
        /// 判断两个实例解密后明文是否相等。
        /// </summary>
        public static bool operator ==(Obfuscated<T> left, Obfuscated<T> right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 判断两个实例解密后明文是否不等。
        /// </summary>
        public static bool operator !=(Obfuscated<T> left, Obfuscated<T> right)
        {
            return !left.Equals(right);
        }

        //////////////////////////////////////////////////////////////////////////
        /// Object：override

        /// <summary>
        /// 判断指定对象是否为同类型且解密后明文相等。
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is Obfuscated<T> other && Equals(other);
        }

        /// <summary>
        /// 返回解密后明文的哈希码。
        /// </summary>
        public override int GetHashCode()
        {
            return EqualityComparer<T>.Default.GetHashCode(GetPlain());
        }

        /// <summary>
        /// 返回解密后的明文；不输出 key 或密文。
        /// </summary>
        public override string ToString()
        {
            return GetPlain().ToString();
        }
    }

    /// <summary>
    /// <see cref="Obfuscated{T}"/> 在浮点类型上的容差比较。
    /// </summary>
    public static class ObfuscatedExtensions
    {
        /// <summary>
        /// 判断解密后的值与目标差的绝对值是否小于等于容差。
        /// </summary>
        /// <param name="value">混淆值。</param>
        /// <param name="target">目标明文。</param>
        /// <param name="tolerance">容差，单位与明文相同。</param>
        public static bool IsApproximately(this Obfuscated<float> value, float target, float tolerance)
        {
            return Math.Abs(value.Value - target) <= tolerance;
        }

        /// <summary>
        /// 判断解密后的值与目标差的绝对值是否小于等于容差。
        /// </summary>
        /// <param name="value">混淆值。</param>
        /// <param name="target">目标明文。</param>
        /// <param name="tolerance">容差，单位与明文相同。</param>
        public static bool IsApproximately(this Obfuscated<double> value, double target, double tolerance)
        {
            return Math.Abs(value.Value - target) <= tolerance;
        }
    }
}
