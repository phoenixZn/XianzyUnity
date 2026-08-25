using System;

namespace Unity.Mathematics
{
    /// <summary>
    /// CLI 托管数学：替代 Unity.Mathematics.math，供 EaseUtility / MotionData / Adapter 编译运行。
    /// </summary>
    public static class math
    {
        /// <summary>与 Unity.Mathematics 一致的单精度 π。</summary>
        public const float PI = (float)Math.PI;

        /// <summary>线性插值。</summary>
        public static float lerp(float x, float y, float s) => x + (y - x) * s;

        /// <summary>线性插值（双精度）。</summary>
        public static double lerp(double x, double y, double s) => x + (y - x) * s;

        /// <summary>两值取大。</summary>
        public static float max(float a, float b) => Math.Max(a, b);

        /// <summary>两值取大。</summary>
        public static double max(double a, double b) => Math.Max(a, b);

        /// <summary>两值取大。</summary>
        public static int max(int a, int b) => Math.Max(a, b);

        /// <summary>夹取到区间。</summary>
        public static float clamp(float x, float a, float b) => Math.Min(b, Math.Max(a, x));

        /// <summary>夹取到区间。</summary>
        public static double clamp(double x, double a, double b) => Math.Min(b, Math.Max(a, x));

        /// <summary>夹取到区间。</summary>
        public static int clamp(int x, int a, int b) => Math.Min(b, Math.Max(a, x));

        /// <summary>向下取整。</summary>
        public static float floor(float x) => (float)Math.Floor(x);

        /// <summary>向下取整。</summary>
        public static double floor(double x) => Math.Floor(x);

        /// <summary>向上取整。</summary>
        public static float ceil(float x) => (float)Math.Ceiling(x);

        /// <summary>向上取整。</summary>
        public static double ceil(double x) => Math.Ceiling(x);

        /// <summary>向零截断。</summary>
        public static float trunc(float x) => (float)Math.Truncate(x);

        /// <summary>向零截断。</summary>
        public static double trunc(double x) => Math.Truncate(x);

        /// <summary>四舍五入到最近整数（银行家舍入与 Math.Round 默认一致）。</summary>
        public static float round(float x) => (float)Math.Round(x);

        /// <summary>四舍五入到最近整数。</summary>
        public static double round(double x) => Math.Round(x);

        /// <summary>浮点取模。</summary>
        public static float fmod(float x, float y) => x % y;

        /// <summary>浮点取模。</summary>
        public static double fmod(double x, double y) => x % y;

        /// <summary>正弦。</summary>
        public static float sin(float x) => (float)Math.Sin(x);

        /// <summary>余弦。</summary>
        public static float cos(float x) => (float)Math.Cos(x);

        /// <summary>幂。</summary>
        public static float pow(float x, float y) => (float)Math.Pow(x, y);

        /// <summary>平方根。</summary>
        public static float sqrt(float x) => (float)Math.Sqrt(x);
    }
}
