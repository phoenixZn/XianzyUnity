using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 待机：随机等待 1~5 秒后进入 Move。
    /// </summary>
    public class DemoStateIdle : MainStateBase
    {
        // 本次待机时长（秒）
        private float _wait;
        // 已等待时长（秒）
        private float _elapsed;

        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        /// <summary>
        /// 进入时抽取本次待机时长。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            _wait = G.Random.RandFloat(1f, 5f);
            _elapsed = 0f;
        }

        /// <summary>
        /// 累计待机时间，到期切 Move。
        /// </summary>
        public override float Update(float dt)
        {
            base.Update(dt);
            _elapsed += dt;
            if (_elapsed >= _wait)
                ChooseNextState("MST_Move");
            return dt;
        }
    }
}
