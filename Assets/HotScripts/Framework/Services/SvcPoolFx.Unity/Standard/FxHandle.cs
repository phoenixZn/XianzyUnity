using System;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 特效票据。按值拷贝仍指向同一条特效，任一拷贝 Stop 后全部失效。
    /// CreateId 为 0 是空句柄；超出存活上限时 CreateId 非 0 且 IsValid 为 false。
    /// </summary>
    public readonly struct FxHandle : IEquatable<FxHandle>
    {
        // 没有实例时的槽下标（超出存活上限）
        internal const int NoSlot = -1;

        // 全局自增创建号；0 表示空句柄
        public readonly int CreateId;

        // 槽下标
        internal readonly int Slot;

        internal FxHandle(int createId, int slot)
        {
            CreateId = createId;
            Slot = slot;
        }

        /// <summary>
        /// 槽位仍对应该创建号，且实例还在。
        /// </summary>
        public bool IsValid => G.FxPool.IsValid(this);

        /// <summary>
        /// 归还到对象池。
        /// </summary>
        public void Stop()
        {
            G.FxPool.Stop(this);
        }

        /// <summary>
        /// 设置世界坐标。
        /// </summary>
        public void SetWorldPosition(Vector3 position)
        {
            G.FxPool.SetWorldPosition(this, position);
        }

        /// <summary>
        /// 设置相对当前父节点的本地坐标。
        /// </summary>
        public void SetLocalPosition(Vector3 localPosition)
        {
            G.FxPool.SetLocalPosition(this, localPosition);
        }

        /// <summary>
        /// 设置世界旋转。
        /// </summary>
        public void SetWorldRotation(Quaternion rotation)
        {
            G.FxPool.SetWorldRotation(this, rotation);
        }

        /// <summary>
        /// 设置相对当前父节点的本地旋转。
        /// </summary>
        public void SetLocalRotation(Quaternion localRotation)
        {
            G.FxPool.SetLocalRotation(this, localRotation);
        }

        /// <summary>
        /// 设置根节点本地缩放。
        /// </summary>
        public void SetLocalScale(Vector3 scale)
        {
            G.FxPool.SetLocalScale(this, scale);
        }

        /// <summary>
        /// 挂到父节点下。父节点之后被销毁时，这条特效会在帧末被收回。
        /// </summary>
        /// <param name="parent">挂点</param>
        /// <param name="worldPositionStays">为 true 时保持当前世界位姿</param>
        public void Attach(Transform parent, bool worldPositionStays = true)
        {
            G.FxPool.Attach(this, parent, worldPositionStays);
        }

        /// <summary>
        /// 从父节点摘下，挂回特效池根节点并保持世界坐标。
        /// </summary>
        public void Detach()
        {
            G.FxPool.Detach(this);
        }

        /// <summary>
        /// 从现在起重设自动回收时间。小于等于 0 则改为一直播放到 Stop 或父节点失效。
        /// </summary>
        public void ResetLifetime(float lifetimeSec)
        {
            G.FxPool.ResetLifetime(this, lifetimeSec);
        }

        /// <summary>
        /// 创建号与槽下标都相同。
        /// </summary>
        public bool Equals(FxHandle other)
        {
            return CreateId == other.CreateId && Slot == other.Slot;
        }

        /// <summary>
        /// 创建号与槽下标都相同。
        /// </summary>
        public override bool Equals(object obj)
        {
            return obj is FxHandle other && Equals(other);
        }

        /// <summary>
        /// 以创建号作为哈希。
        /// </summary>
        public override int GetHashCode()
        {
            return CreateId;
        }

        /// <summary>
        /// 创建号与槽下标都相同。
        /// </summary>
        public static bool operator ==(FxHandle left, FxHandle right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// 创建号或槽下标不同。
        /// </summary>
        public static bool operator !=(FxHandle left, FxHandle right)
        {
            return !left.Equals(right);
        }
    }
}
