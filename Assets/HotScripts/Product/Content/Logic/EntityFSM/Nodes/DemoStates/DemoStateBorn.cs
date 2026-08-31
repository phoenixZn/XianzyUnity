using UnityEngine;
using Xease;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 出生：挂 Transform、异步加载 ActorCube+ActorSphere，就绪后绑定 Collider GO 再进入 Idle。
    /// </summary>
    public class DemoStateBorn : MainStateBase
    {
        // YooAsset location；DemoModeLoading 预热与 Born 加载共用
        internal const string DemoViewAssetCube = "ActorCube";
        internal const string DemoViewAssetSphere = "ActorSphere";

        //////////////////////////////////////////////////////////////////////////
        /// CustomBhvState：override

        public override void Destroy()
        {
            base.Destroy();
        }

        /// <summary>
        /// 随机落点并请求异步加载两套表现；GO 位姿由 Transform 同步。
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
            _ownerEntity.AddComCommandSender(new EntityCmdPreHandler_SimpleImmediately());
            //_ownerEntity.RequestViewLoad<AsyncAssetViewWrapper>(DemoViewAssetCube);
            _ownerEntity.RequestViewLoad<PooledAssetViewWrapper>(DemoViewAssetCube);
            _ownerEntity.RequestViewLoad<PooledAssetViewWrapper>(DemoViewAssetSphere);
        }

        /// <summary>
        /// 轮询 View 加载结束（Ready 或 Failed）后再切 Idle；全部成功则绑定 Collider。
        /// </summary>
        public override float Update(float dt)
        {
            base.Update(dt);
            if (!TryGetViewLoadSettled(out var allReady))
                return dt;

            if (!allReady)
            {
                ChooseNextState("MST_Idle");
                return dt;
            }

            BindActorSphereUnityObjects();
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
                var acquirable = acquirables[i];
                var state = acquirable.LoadState;
                if (state == ViewLoadState.None || state == ViewLoadState.Loading)
                    return false;
                if (state == ViewLoadState.Ready)
                    continue;

                allReady = false;
                // 已结束且非 Ready：点出具体资源，避免笼统报一个名字
                var asset = acquirable is IViewAssetLocatable loc ? loc.AssetLocation : acquirable.GetType().Name;
                this.LogError($"DemoStateBorn View load failed, asset={asset}, state={state}");
            }

            return true;
        }

        // 所有已加载 View 下的 Collider GO 都 Bind，射线命中任意碰撞盒都能反查 entity
        private void BindActorSphereUnityObjects()
        {
            if (_ownerEntity == null || !_ownerEntity.hasComView)
                return;

            var wrappers = _ownerEntity.comView.Wrappers;
            for (int i = 0; i < wrappers.Count; ++i)
            {
                if (wrappers[i] is not IViewGameObjectHolder holder)
                    continue;

                var go = holder.Instance;
                if (go == null)
                    continue;

                var colliders = go.GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < colliders.Length; ++c)
                    _ownerEntity.BindUnityObject(colliders[c].gameObject);
            }
        }
    }
}
