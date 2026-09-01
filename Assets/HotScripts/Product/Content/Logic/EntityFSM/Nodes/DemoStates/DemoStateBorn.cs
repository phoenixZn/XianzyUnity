using UnityEngine;
#if !CONSOLE_CLIENT
using Xease.ModelPointTool;
#endif

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 出生：加载 ActorCube+ActorSphere，就绪后将 Sphere 挂到 Cube 的 Head，绑定 Collider 再进入 Idle。
    /// </summary>
    public class DemoStateBorn : MainStateBase
    {
        // YooAsset location；DemoModeLoading 预热与 Born 加载共用
        internal const string DemoViewAssetCube = "ActorCube";
        internal const string DemoViewAssetSphere = "ActorSphere";
#if !CONSOLE_CLIENT
        // ActorCube 上用于挂载 Sphere 的挂点名（与生成表一致）
        private const string CubeHeadPoint = "Head";
#endif

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
            _ownerEntity.AddComCommandSender<EntityCmdPreHandler_SimpleImmediately>();
            //_ownerEntity.RequestViewLoad<AsyncAssetViewWrapper>(DemoViewAssetCube);
            _ownerEntity.RequestViewLoad<PooledAssetViewWrapper>(DemoViewAssetCube);
            _ownerEntity.RequestViewLoad<PooledAssetViewWrapper>(DemoViewAssetSphere);
        }

        /// <summary>
        /// 轮询 View 加载结束（Ready 或 Failed）后再切 Idle；全部成功则挂接 Head 并绑定 Collider。
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

            AttachSphereToCubeHead();
            BindColliderUnityObjects();
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

        // Sphere 挂到 Cube 的 Head，并关闭 Sphere 的逻辑 Transform 同步
        private void AttachSphereToCubeHead()
        {
#if !CONSOLE_CLIENT
            if (!TryGetLoadedView(DemoViewAssetCube, out var cubeGo, out _)
                || !TryGetLoadedView(DemoViewAssetSphere, out var sphereGo, out var sphereSync))
            {
                this.LogError("DemoStateBorn attach failed: cube or sphere view missing");
                return;
            }

            var head = ModelPointGetter.FindBindPoint(cubeGo.transform, DemoViewAssetCube, CubeHeadPoint);
            if (head == null)
            {
                this.LogError($"DemoStateBorn attach failed: {CubeHeadPoint} not found on {DemoViewAssetCube}");
                return;
            }

            sphereGo.transform.SetParent(head, false);
            sphereGo.transform.localPosition = Vector3.zero;
            sphereGo.transform.localRotation = Quaternion.identity;
            if (sphereSync != null)
                sphereSync.NeedsSyncTransform = false;
#endif
        }

#if !CONSOLE_CLIENT
        // 按 AssetLocation 取已加载 GO；sync 为同一 wrapper 的 IViewTransformSyncable（可空）
        private bool TryGetLoadedView(string location, out GameObject go, out IViewTransformSyncable sync)
        {
            go = null;
            sync = null;
            if (_ownerEntity == null || !_ownerEntity.hasComView)
                return false;

            var wrappers = _ownerEntity.comView.Wrappers;
            for (int i = 0; i < wrappers.Count; ++i)
            {
                var wrapper = wrappers[i];
                if (wrapper is not IViewAssetLocatable loc || loc.AssetLocation != location)
                    continue;
                if (wrapper is not IViewGameObjectHolder holder || holder.Instance == null)
                    continue;

                go = holder.Instance;
                sync = wrapper as IViewTransformSyncable;
                return true;
            }

            return false;
        }
#endif

        // 所有已加载 View 下的 Collider GO 都 Bind，射线命中任意碰撞盒都能反查 entity
        private void BindColliderUnityObjects()
        {
            //this.Log($"BindColliderUnityObjects");
#if !CONSOLE_CLIENT
            if (_ownerEntity == null || !_ownerEntity.hasComView)
                return;

            var wrappers = _ownerEntity.comView.Wrappers;
            // List 重载 + ListPool：不分配 Collider[]；using/Get(out) 结束时 Release
            using (UnityEngine.Pool.ListPool<Collider>.Get(out var colliders))
            {
                for (int i = 0; i < wrappers.Count; ++i)
                {
                    if (wrappers[i] is not IViewGameObjectHolder holder)
                        continue;

                    var go = holder.Instance;
                    if (go == null)
                        continue;

                    go.GetComponentsInChildren(true, colliders);
                    for (int c = 0; c < colliders.Count; ++c)
                        _ownerEntity.RelateToUnityObject(colliders[c].gameObject);
                }
            }
#endif
        }
    }
}
