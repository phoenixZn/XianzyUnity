using System;
using System.Collections;
using System.Collections.Generic;
using YooAsset;

namespace HotUpdate
{
    //////////////////////////////////////////////////////////////////////////
    // Service：资源管理
    public partial class AssetService : IAssetService
    {
        private Dictionary<EAssetGroup, AssetLoader> _loader = new ();

        public void Init(string defaultPakName, string rawFilePakName)
        {
            DefaultPackage = YooAssets.GetPackage(defaultPakName);
            RawFilePackage = YooAssets.GetPackage(rawFilePakName);
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService
        public void Reset()
        {
            Release();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IAssetService
        public ResourcePackage DefaultPackage { get; protected set; }
        public ResourcePackage RawFilePackage { get; protected set; }

        public void Release()
        {
            foreach (var loader in _loader.Values)
                loader.Release();
            _loader.Clear();
        }

        public void Release(EAssetGroup group)
        {
            if (_loader.TryGetValue(group, out var loader))
                loader.Release();
            _loader.Remove(group);
        }

        public void Release(string location, EAssetGroup group = EAssetGroup.Default)
        {
            if (_loader.TryGetValue(group, out var loader))
                loader.Release(location);
        }

        public void LoadAssetAsync(string location, System.Action<AssetHandle> callback, EAssetGroup group = EAssetGroup.Default, uint priority = 0)
        {
            GetOrCreateLoader(group).LoadAssetAsync(location, callback, priority);
        }
        
        public void LoadAssetAsync<T>(string location, System.Action<AssetHandle> callback, EAssetGroup group = EAssetGroup.Default, uint priority = 0) where T : UnityEngine.Object
        {
            GetOrCreateLoader(group).LoadAssetAsync<T>(location, callback, priority);
        }
        
        public IEnumerator LoadAssetAsync(string location, EAssetGroup group = EAssetGroup.Default, System.Action<AssetHandle> onBegin = null, uint priority = 0)
        {
            return GetOrCreateLoader(group).LoadAssetCoro(location, onBegin, priority);
        }

        public IEnumerator LoadAssetAsync<T>(string location, EAssetGroup group = EAssetGroup.Default, System.Action<AssetHandle> onBegin = null, uint priority = 0) where T : UnityEngine.Object
        {
            return GetOrCreateLoader(group).LoadAssetCoro<T>(location, onBegin, priority);
        }

        public AssetHandle LoadAssetSync(string location, EAssetGroup group = EAssetGroup.Default)
        {
            return GetOrCreateLoader(group).LoadAssetSync(location);
        }

        public AssetHandle LoadAssetSync<T>(string location, EAssetGroup group = EAssetGroup.Default) where T : UnityEngine.Object
        {
            return GetOrCreateLoader(group).LoadAssetSync<T>(location);
        }
        
        public RawFileHandle LoadAssetRawFileSync(string location, EAssetGroup group = EAssetGroup.Default)
        {
            return GetOrCreateLoader(group).LoadAssetRawFileSync(location);
        }

        private AssetLoader GetOrCreateLoader(EAssetGroup group)
        {
            if (_loader.TryGetValue(group, out var loader))
                return loader;
            loader = new AssetLoader();
            _loader.Add(group, loader);
            return loader;
        }

    }
}
