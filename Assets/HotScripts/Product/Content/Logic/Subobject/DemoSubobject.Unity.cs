using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xease.CoreGame
{
    internal static partial class DemoSubobject
    {
        // YooAsset 资源地址，命中确认后通过特效池生成。
        private const string HitFxLocation = "Map-Hit";
        // 命中特效的自动回收时长，秒。
        private const float HitFxLifetimeSec = 2f;

        //////////////////////////////////////////////////////////////////////////
        /// This：

        // 独立于子弹和目标的 View，命中回收或致死销毁不会提前移除特效。
        static partial void PlayHitFx(Vector3 position)
        {
            if (G.FxPool == null)
                return;

            G.FxPool.SpawnAsync(HitFxLocation, position, HitFxLifetimeSec).Forget();
        }
    }
}
