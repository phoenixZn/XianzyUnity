using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// Demo 出生：加载身体与装饰部件，就绪后将装饰部件挂到主视图挂点，绑定 Collider 再进入 Idle。
    /// </summary>
    public partial class DemoStateBorn : MainStateBase
    {
        // YooAsset location；DemoModeLoading 预热与 Born 加载共用
        internal const string DemoViewAssetBody = "ActorCube";
        internal const string DemoViewAssetOrnament = "Bats-T";//"ActorSphere";

        // 身体上用于挂载装饰部件的挂点名（与生成表一致）
        private const string BodyBindPoint = "Head";


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
            PlayBornAudio();
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

        // 出生音效；命令行宿主不注册音频服务，无实现时调用被抹掉
        partial void PlayBornAudio();

        // 装饰部件挂到主视图挂点，并关闭装饰部件的逻辑 Transform 同步；无实现时调用被抹掉
        partial void AttachOrnamentToMainViewBind();

        // 已加载 View 下的 Collider GO 都 Bind，射线命中碰撞盒能反查 entity；无实现时调用被抹掉
        partial void BindColliderUnityObjects();
    }
}
