using System.Collections;
using System.Collections.Generic;
using YooAsset;

namespace Xease
{
    public class AssetLoader : IAssetLoader
    {
        private readonly Dictionary<string, AssetHandle> _assetHandles = new ();
        private Dictionary<string, RawFileHandle> _rawFileHandles;

        protected ResourcePackage DefaultPackage => G.Asset.DefaultPackage;

        protected ResourcePackage RawFilePackage => G.Asset.RawFilePackage;

        
        public virtual void Dispose() => Release();
        
        public void Release()
        {
            if (_assetHandles != null)
            {
                foreach (var handle in _assetHandles)
                {
                    handle.Value.Release();
                }
                _assetHandles.Clear();
            }
            if (_rawFileHandles != null)
            {
                foreach (var handle in _rawFileHandles)
                {
                    handle.Value.Release();
                }
                _rawFileHandles.Clear();
            }
        }

        public void Release(string location)
        {
            if (_assetHandles != null && _assetHandles.TryGetValue(location, out var handle))
            {
                handle.Release();
                _assetHandles.Remove(location);
            }
            else
            {
                if (_rawFileHandles == null || !_rawFileHandles.TryGetValue(location, out var _handle)) return;
                _handle.Release();
                _rawFileHandles.Remove(location);
            }
        }

        public AssetHandle TryGetAsset(string location)
        {
            return _assetHandles.TryGetValue(location, out var handle) ? handle : null;
        }

        public AssetHandle LoadAssetAsync(string location, System.Action<AssetHandle> callback, uint priority = 0)
        {
            if (_assetHandles.TryGetValue(location, out var handle))
            {
                if (handle.IsDone)
                {
                    callback?.Invoke(handle);
                    return handle;
                }
                if (callback != null)
                    handle.Completed += callback;
            }
            else
            {
                handle = DefaultPackage.LoadAssetAsync(location, priority);
                if (callback != null)
                    handle.Completed += callback;
                _assetHandles.Add(location, handle);
            }

            return handle;
        }

        public AssetHandle LoadAssetAsync<T>(string location, System.Action<AssetHandle> onCompleted, uint priority = 0) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(location, out var handle))
            {
                if (handle.IsDone)
                {
                    onCompleted?.Invoke(handle);
                    return handle;
                }
                if (onCompleted != null)
                    handle.Completed += onCompleted;
            }
            else
            {
                handle = DefaultPackage.LoadAssetAsync(location, typeof(T), priority);
                if (onCompleted != null)
                    handle.Completed += onCompleted;
                _assetHandles.Add(location, handle);
            }

            return handle;
        }

        public IEnumerator LoadAssetCoro(string location, System.Action<AssetHandle> onBegin = null, uint priority = 0)
        {
            if (_assetHandles.TryGetValue(location, out var handle))
            {
                onBegin?.Invoke(handle);
                while (!handle.IsDone)
                    yield return null;
            }
            else
            {
                handle = DefaultPackage.LoadAssetAsync(location, priority);
                _assetHandles.Add(location, handle);
                onBegin?.Invoke(handle);
            }
            yield return handle;
        }

        public IEnumerator LoadAssetCoro<T>(string location, System.Action<AssetHandle> onBegin = null, uint priority = 0) where T : UnityEngine.Object
        {
            if (_assetHandles.TryGetValue(location, out var handle))
            {
                onBegin?.Invoke(handle);
                while (!handle.IsDone)
                    yield return null;
            }
            else
            {
                handle = DefaultPackage.LoadAssetAsync(location, typeof(T), priority);
                _assetHandles.Add(location, handle);
                onBegin?.Invoke(handle);
            }
            yield return handle;
        }

        // 只用于加载小资产，Web平台此接口内部会异步接口并同步等待
        public AssetHandle LoadAssetSync(string location)
        {
            if (_assetHandles.TryGetValue(location, out var handle))
            {
                return handle;
            }
            handle = DefaultPackage.LoadAssetSync(location);
            _assetHandles.Add(location, handle);
            return handle;
        }

        // 只用于加载小资产，Web平台此接口内部会异步接口并同步等待
        public AssetHandle LoadAssetSync<T>(string location)
        {
            if (_assetHandles.TryGetValue(location, out var handle))
            {
                return handle;
            }
            handle = DefaultPackage.LoadAssetSync(location, typeof(T));
            _assetHandles.Add(location, handle);
            return handle;
        }
        
        public RawFileHandle LoadAssetRawFileSync(string location)
        {
            _rawFileHandles??= new Dictionary<string, RawFileHandle>();
            if (_rawFileHandles.TryGetValue(location, out var handle))
            {
                return handle;
            }
            handle = RawFilePackage.LoadRawFileSync(location);
            _rawFileHandles.Add(location, handle);
            return handle;
        }
    }
}