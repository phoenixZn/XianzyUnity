using Cysharp.Threading.Tasks;

namespace Xease.CoreGame
{
    public partial class DemoStateDie
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        // YooAsset location，死亡烟雾。
        private const string DeathFxLocation = "FireNinja_Smoke";
        // 自动回收时长，秒；覆盖烟雾和火花的粒子尾迹。
        private const float DeathFxLifetimeSec = 1.2f;

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
