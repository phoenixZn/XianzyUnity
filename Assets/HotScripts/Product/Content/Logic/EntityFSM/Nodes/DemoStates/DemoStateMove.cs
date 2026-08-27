using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 移动：在边界内随机选点并匀速走过去，到达后回 Idle。
    /// </summary>
    public class DemoStateMove : MainStateBase
    {
        // 匀速移动速度（世界单位/秒）
        private const float MoveSpeed = 4f;
        // 判定到达的距离阈值
        private const float ArriveDist = 0.1f;
        // 测试活动范围：x/y ∈ [BoundMin, BoundMax]，z=0
        private const float BoundMin = -10f;
        private const float BoundMax = 10f;

        // 本次移动目标点（z 恒为 0）
        private Vector3 _target;

        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        /// <summary>
        /// 进入时在边界内随机一个目标点。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            _target = new Vector3(
                G.Random.RandFloat(BoundMin, BoundMax),
                G.Random.RandFloat(BoundMin, BoundMax),
                0f);
        }

        /// <summary>
        /// 朝目标匀速移动，到达后切回 Idle。
        /// </summary>
        public override float Update(float dt)
        {
            base.Update(dt);
            if (_ownerEntity == null || !_ownerEntity.hasComTransform)
                return dt;

            var pos = _ownerEntity.position;
            var to = _target - pos;
            var dist = to.magnitude;
            var step = MoveSpeed * dt;
            if (dist <= ArriveDist || dist <= step)
            {
                _ownerEntity.SetPosition(ClampToBound(_target));
                ChooseNextState("MST_Idle");
                return dt;
            }

            var next = pos + to / dist * step;
            _ownerEntity.SetPosition(ClampToBound(next));
            return dt;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 限制在 x/y ∈ [-10,10]、z=0，避免走出测试范围
        private static Vector3 ClampToBound(Vector3 pos)
        {
            pos.x = Mathf.Clamp(pos.x, BoundMin, BoundMax);
            pos.y = Mathf.Clamp(pos.y, BoundMin, BoundMax);
            pos.z = 0f;
            return pos;
        }
    }
}
