using Cysharp.Threading.Tasks;

namespace Xease.CoreGame
{
    public partial class DemoStateDie
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // YooAsset location，死亡爆炸
        private const string DeathFxLocation = "Map-Hit";
        // 自动回收时长，秒
        private const float DeathFxLifetimeSec = 0.5f;

        // 世界坐标生成，不挂到即将销毁的 View
        partial void PlayDeathFx()
        {
            if (_ownerEntity == null || !_ownerEntity.hasComTransform)
                return;
            if (G.FxPool == null)
            {
                this.LogError("DemoStateDie FxPool is null");
                return;
            }

            G.FxPool.SpawnAsync(DeathFxLocation, _ownerEntity.position, DeathFxLifetimeSec).Forget();
        }
    }
}
