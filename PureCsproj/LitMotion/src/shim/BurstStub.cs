using System;

namespace Unity.Burst
{
    /// <summary>
    /// CLI 空属性：Unity 侧交给 Burst 编译器，命令行忽略。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class BurstCompileAttribute : Attribute
    {
    }
}

namespace Unity.Burst.CompilerServices
{
    /// <summary>
    /// CLI 空实现：Likely/Unlikely 原样返回条件，不产生分支提示。
    /// </summary>
    public static class Hint
    {
        /// <summary>
        /// 原样返回条件（无分支预测提示）。
        /// </summary>
        public static bool Likely(bool condition) => condition;

        /// <summary>
        /// 原样返回条件（无分支预测提示）。
        /// </summary>
        public static bool Unlikely(bool condition) => condition;
    }
}
