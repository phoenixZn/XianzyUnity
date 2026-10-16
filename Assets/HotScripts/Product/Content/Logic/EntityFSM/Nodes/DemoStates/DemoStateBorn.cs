using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 出生：加载身体与装饰部件，就绪后将装饰部件挂到主视图挂点，绑定 Collider 再进入 Idle。
    /// </summary>
    public class DemoStateBorn : MainStateBase
    {
        // YooAsset location；DemoModeLoading 预热与 Born 加载共用
        internal const string DemoViewAssetBody = "ActorCube";
        internal const string DemoViewAssetOrnament = "Bats-T";//"ActorSphere";
#if !CONSOLE_CLIENT
        // 身体上用于挂载装饰部件的挂点名（与生成表一致）
        private const string BodyBindPoint = "Head";
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
            //_ownerEntity.RequestViewLoad<AsyncAssetViewWrapper>(DemoViewAssetBody);
            _ownerEntity.RequestViewLoad<PooledAssetViewWrapper>(DemoViewAssetBody);
            _ownerEntity.RequestViewLoad<PooledAssetViewWrapper>(DemoViewAssetOrnament);
            
#if !CONSOLE_CLIENT
            // 命令行宿主不注册音频服务
            G.Audio.Play("AudioBankCombat", "bounce");
#endif
        }

        /// <summary>
        /// 轮询 View 加载结束（Ready 或 Failed）后再切 Idle；全部成功则把装饰部件挂到主视图挂点并绑定 Collider。
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

            AttachOrnamentToMainViewBind();
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

        // 装饰部件挂到主视图挂点，并关闭装饰部件的逻辑 Transform 同步
        private void AttachOrnamentToMainViewBind()
        {
#if !CONSOLE_CLIENT
            if (!TryGetLoadedView(DemoViewAssetOrnament, out var ornamentProxy, out var ornamentSync))
            {
                this.LogError("DemoStateBorn attach failed: ornament view missing");
                return;
            }

            var mount = _ownerEntity.GetMainViewBindTransform(BodyBindPoint);
            // 空挂点名返回当前绑定根；与挂点结果相同表示未登记或未找到
            var mainRoot = _ownerEntity.GetMainViewBindTransform(null);
            if (mount == null || mount == mainRoot)
            {
                this.LogError($"DemoStateBorn attach failed: {BodyBindPoint} not found on main view");
                return;
            }

            var ornamentRoot = ornamentProxy.GetBindPoint(null);
            if (ornamentRoot == null)
            {
                this.LogError("DemoStateBorn attach failed: ornament root missing");
                return;
            }

            ornamentRoot.SetParent(mount, false);
            ornamentRoot.localPosition = Vector3.zero;
            ornamentRoot.localRotation = Quaternion.identity;
            if (ornamentSync != null)
                ornamentSync.NeedsSyncTransform = false;
#endif
        }

#if !CONSOLE_CLIENT
        // 按 AssetLocation 取已绑定的 Unity 代理；sync 为同一 wrapper 的 IViewTransformSyncable（可空）
        private bool TryGetLoadedView(string location, out UnityViewTransformProxy proxy, out IViewTransformSyncable sync)
        {
            proxy = null;
            sync = null;
            if (_ownerEntity == null || !_ownerEntity.hasComView)
                return false;

            var wrappers = _ownerEntity.comView.Wrappers;
            for (int i = 0; i < wrappers.Count; ++i)
            {
                var wrapper = wrappers[i];
                if (wrapper is not IViewAssetLocatable loc || loc.AssetLocation != location)
                    continue;
                if (wrapper is not ViewWrapperBase view || view.Proxy is not UnityViewTransformProxy unity || !unity.IsValid)
                    continue;

                proxy = unity;
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
