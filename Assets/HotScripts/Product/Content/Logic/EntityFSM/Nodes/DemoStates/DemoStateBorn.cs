using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 出生：挂 Transform、异步加载 View，完成后进入 Idle。
    /// </summary>
    public class DemoStateBorn : MainStateBase
    {
        // YooAsset location，与 GoPool 演示预制体一致
        private const string DemoViewAsset = "ActorCube";

        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        /// <summary>
        /// 随机落点并请求异步加载表现；GO 位姿由 Transform 同步。
        /// </summary>
        public override void Enter()
        {
            base.Enter();
            if (_ownerEntity == null)
                return;

            var pos = new Vector3(
                G.Random.RandFloat(-10f, 10f),
                G.Random.RandFloat(-10f, 10f),
                0f);
            _ownerEntity.SetPosition(pos);
            _ownerEntity.RequestViewLoad(DemoViewAsset);
            _ownerEntity.RequestViewLoad("ActorSphere");
        }

        /// <summary>
        /// 轮询 View 加载结束（Ready 或 Failed）后再切 Idle。
        /// </summary>
        public override float Update(float dt)
        {
            base.Update(dt);
            if (!TryGetViewLoadSettled(out var allReady))
                return dt;

            if (!allReady)
                this.LogError($"DemoStateBorn View load failed, asset={DemoViewAsset}");

            ChooseNextState("MST_Idle");
            return dt;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // true=加载结束（Ready/Failed）；false=尚未挂 View 或仍在 None/Loading
        private bool TryGetViewLoadSettled(out bool allReady)
        {
            allReady = false;
            if (_ownerEntity == null || !_ownerEntity.hasComView)
                return false;

            var acquirables = _ownerEntity.comView.Acquirables;
            if (acquirables.Count == 0)
                return false;

            allReady = true;
            for (int i = 0; i < acquirables.Count; ++i)
            {
                var state = acquirables[i].LoadState;
                if (state == ViewLoadState.None || state == ViewLoadState.Loading)
                    return false;
                if (state != ViewLoadState.Ready)
                    allReady = false;
            }

            return true;
        }
    }
}
