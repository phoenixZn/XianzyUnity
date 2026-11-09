using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 待机：随机等待后进入 Move，期间按 DemoSubobject.FireInterval 向 XY 平面随机发射子物体。
    /// </summary>
    public class DemoStateIdle : MainStateBase
    {
        // 抵消累计帧时间在发射间隔边界上的浮点误差，单位为秒。
        private const float FireTimeEpsilon = 1e-6f;
        // 本次待机时长（秒）
        private float _wait;
        // 已等待时长（秒）
        private float _elapsed;
        // 本次待机累计的发射余量，秒；退出待机后不累积。
        private float _fireElapsed;

        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        public override void Destroy()
        {
            _wait = 0f;
            _elapsed = 0f;
            _fireElapsed = 0f;
            base.Destroy();
        }

        /// <summary>
        /// 进入时抽取本次待机时长。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            _wait = G.Random.RandFloat(1f, 5f);
            _elapsed = 0f;
            _fireElapsed = 0f;
        }

        /// <summary>
        /// 累计待机时间并按固定间隔发射；跨过状态结束的帧只计算仍在待机的时间。
        /// </summary>
        public override float Update(float dt)
        {
            base.Update(dt);
            var idleDt = System.Math.Min(dt, System.Math.Max(0f, _wait - _elapsed));
            _fireElapsed += idleDt;
            // 保留发射余量，低帧率跨过多个间隔时补发，避免频率随帧率下降。
            while (_fireElapsed + FireTimeEpsilon >= DemoSubobject.FireInterval)
            {
                // 容差触发时清掉极小负余量，避免边界误差在长期待机中持续累积。
                _fireElapsed = System.Math.Max(0f, _fireElapsed - DemoSubobject.FireInterval);
                DemoSubobject.Fire(_ownerEntity, this.GetMetaWorld().comUniSubobjects);
            }
            _elapsed += dt;
            if (_elapsed >= _wait)
                ChooseNextState("MST_Move");
            return dt;
        }
    }
}
