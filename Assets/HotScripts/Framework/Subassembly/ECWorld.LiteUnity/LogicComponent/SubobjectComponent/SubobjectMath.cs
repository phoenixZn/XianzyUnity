using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 子物体用到的向量与旋转运算。只用托管实现：命令行宿主下 Quaternion.FromToRotation 等引擎原生方法不可用。
    /// </summary>
    public static class SubobjectMath
    {
        // 视为零向量的平方长度
        private const float ZeroSqrEpsilon = 1e-10f;
        // 点积低于此值视为反向共线，最短弧旋转改绕任意垂直轴转半圈
        private const float OppositeDot = -0.999999f;

        /// <summary>
        /// 把单位向量 from 朝单位向量 to 旋转，最多转 maxRadians，返回单位向量。
        /// </summary>
        public static Vector3 RotateTowards(Vector3 from, Vector3 to, float maxRadians)
        {
            var dot = Mathf.Clamp(Vector3.Dot(from, to), -1f, 1f);
            var angle = Mathf.Acos(dot);
            if (angle <= maxRadians)
                return to;

            // from 与 to 张成平面内、垂直于 from 的方向；反向共线时任取一条垂线
            var perp = to - from * dot;
            if (perp.sqrMagnitude < ZeroSqrEpsilon)
                perp = AnyPerpendicular(from);
            perp.Normalize();
            return from * Mathf.Cos(maxRadians) + perp * Mathf.Sin(maxRadians);
        }

        /// <summary>
        /// 把单位向量 from 转到单位向量 to 的最短弧旋转。
        /// </summary>
        public static Quaternion FromToRotation(Vector3 from, Vector3 to)
        {
            var dot = Vector3.Dot(from, to);
            if (dot < OppositeDot)
            {
                var axis = AnyPerpendicular(from).normalized;
                return new Quaternion(axis.x, axis.y, axis.z, 0f);
            }

            var cross = Vector3.Cross(from, to);
            var q = new Quaternion(cross.x, cross.y, cross.z, 1f + dot);
            return Normalize(q);
        }

        /// <summary>
        /// 计算 source 与 target 两个阵营的关系。任一方中立或未设置阵营时为 Neutral。
        /// </summary>
        public static ESubobjectFactionFilter Relation(EFaction source, EFaction target)
        {
            if (source == EFaction.Invalid || source == EFaction.Neutral || target == EFaction.Invalid || target == EFaction.Neutral)
                return ESubobjectFactionFilter.Neutral;
            return source == target ? ESubobjectFactionFilter.Ally : ESubobjectFactionFilter.Hostile;
        }

        // 取一条与 v 垂直的向量（未归一化）
        private static Vector3 AnyPerpendicular(Vector3 v)
        {
            var axis = Mathf.Abs(v.x) < 0.9f ? Vector3.right : Vector3.up;
            return Vector3.Cross(v, axis);
        }

        private static Quaternion Normalize(Quaternion q)
        {
            var mag = Mathf.Sqrt(q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w);
            if (mag < Mathf.Epsilon)
                return Quaternion.identity;
            var inv = 1f / mag;
            return new Quaternion(q.x * inv, q.y * inv, q.z * inv, q.w * inv);
        }
    }
}
