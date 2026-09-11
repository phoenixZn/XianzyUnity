using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 移动：在边界内随机选点并匀速走过去，到达后回 Idle；途中 Nt_Death 则到达后再进 Die。
    /// </summary>
    public class DemoStateMove : MainStateBase
    {
        // 默认匀速（世界单位/秒）；点击后在此基础上翻倍
        private const float MoveSpeed = 2f;
        // 判定到达的距离阈值
        private const float ArriveDist = 0.1f;
        // 测试活动范围：x/y ∈ [BoundMin, BoundMax]，z=0
        private const float BoundMin = -10f;
        private const float BoundMax = 10f;

        // 本次移动目标点（z 恒为 0）
        private Vector3 _target;
        // 本次实际移速；Enter 重置为 MoveSpeed，点击后 * 2
        private float _moveSpeed;
        // 途中收到 Nt_Death，到达当前目标后再切 Die
        private bool _pendingDeath;

        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        public override void Destroy()
        {
            _target = default;
            _moveSpeed = 0f;
            _pendingDeath = false;
            base.Destroy();
        }

        /// <summary>
        /// 进入时在边界内随机一个目标点。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            _pendingDeath = false;
            _moveSpeed = MoveSpeed;
            _target = new Vector3(
                G.Random.RandFloat(BoundMin, BoundMax),
                G.Random.RandFloat(BoundMin, BoundMax),
                0f);
        }

        /// <summary>
        /// 朝目标匀速移动；到达后切 Idle，若已挂待死则切 Die。
        /// </summary>
        public override float Update(float dt)
        {
            base.Update(dt);
            if (_ownerEntity == null || !_ownerEntity.hasComTransform)
                return dt;

            var pos = _ownerEntity.position;
            var to = _target - pos;
            var dist = to.magnitude;
            var step = _moveSpeed * dt;
            if (dist <= ArriveDist || dist <= step)
            {
                var arrived = _target;
                _ownerEntity.SetPosition(arrived);
                if (G.IsCLI)
                    this.Log($"DemoStateMove arrive pos={arrived}");
                ChooseNextState(_pendingDeath ? "MST_Die" : "MST_Idle");
                return dt;
            }

            var next = pos + to / dist * step;
            _ownerEntity.SetPosition(next);
            return dt;
        }

        //////////////////////////////////////////////////////////////////////////
        /// MainStateBase：override

        /// <summary>
        /// 拦截 Nt_Death：不立刻切 Die，记下待死等本次移动走完；同时改目标到上方、速度翻倍、绕 z 转 45°。
        /// </summary>
        public override bool HandleEntityCommand(LogicEntity entity, EntityCommand cmd)
        {
            if (cmd.CmdType != EntityCmdType.Nt_Death)
                return base.HandleEntityCommand(entity, cmd);

            if (this.isDebug())
                this.Log($"{StateID}.HandleEntityCommand Nt_Death: pending until arrive");
            _pendingDeath = true;

            if (_ownerEntity == null || !_ownerEntity.hasComTransform)
                return true;

            // 点击：目标改到当前位置上方 2 单位，速度翻倍，绕 z 转 45°
            _target = _ownerEntity.position + Vector3.up * 2f;
            _moveSpeed *= 2f;
            _ownerEntity.SetQuaternion(_ownerEntity.rotation * Quaternion.Euler(0f, 0f, 45f));
            
            var audioEvent = G.Audio.CreateEvent("AudioBankCombat", "bounce");
            audioEvent.Play();
            return true;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        
    }
}
