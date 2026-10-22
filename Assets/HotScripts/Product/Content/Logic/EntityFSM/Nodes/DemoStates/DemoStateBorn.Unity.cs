using UnityEngine;

namespace Xease.CoreGame
{
    public partial class DemoStateBorn
    {
        //////////////////////////////////////////////////////////////////////////
        /// This：

        partial void PlayBornAudio()
        {
            G.Audio.Play("AudioBankCombat", "bounce");
        }

        partial void AttachOrnamentToMainViewBind()
        {
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
        }

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

        partial void BindColliderUnityObjects()
        {
            //this.Log($"BindColliderUnityObjects");
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
        }
    }
}
