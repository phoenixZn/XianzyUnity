/*
 * GameObjectPoolService 需求实现概要
 *
 * 1. 核心设计
 *    - 根节点：运行时创建并挂 DontDestroyOnLoad，缺省名 [GameObjectPool]，
 *      构造可指定如 [GameObjectPool.Core] / [GameObjectPool.Battle]。
 *    - 二级节点（每预制体一对，平级挂在根下）：
 *      PrefabName：空闲未激活实例；PrefabName[Rented]：已租出且调用方未改父节点。
 *    - 子池：每预制体一个 UnityObjectPoolBase<GameObject>（PrefabPool）。
 *
 * 2. 生命周期
 *    - Rent：从分类节点取对象 → SetParent(PrefabName[Rented], false)，保持未激活；
 *      调用方负责 SetActive(true)。
 *    - Return：先 SetActive(false) → SetParent(category, false) 挂回分类节点。
 *    - 挂载/摘离一律 worldPositionStays=false，避免父节点偏移导致坐标突变。
 *
 * 3. 预热与扩容
 *    - Prewarm：Instantiate 后直接未激活并挂分类节点入池，不经 Rent 路径。
 *    - 池空扩容：Instantiate → OnCreate（未激活+挂分类节点）→ OnRent（挂到该类型 PrefabName[Rented]，仍未激活）。
 *    - 满池（MaxSize）：Release 时 Destroy，不挂回。默认 MaxSize=1000；
 *      ApplySettings(<PrefabName, maxSize>) 仅影响此后新建子池。
 *
 * 4. 标识与边界
 *    - 实例名：保留 Instantiate 默认（PrefabName(Clone)），不额外改名。
 *    - 重复 Release：以租出表为准告警并忽略，防嵌套入池。
 *    - Clear(prefab|name|全部)：Destroy 空闲实例并移除分类节点；
 *      PrefabName[Rented] 无子节点才拆，仍有租出实例则保留以免误杀；全部时保留根节点。
 *    - 池键：prefab.name（项目保证预制体名全局唯一）。
 */

using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using MackySoft.XPool.Unity.ObjectModel;
using UnityEngine;
using YooAsset;
using UnityObject = UnityEngine.Object;

namespace Xease
{
    /// <summary>
    /// GameObject 池服务实现：根节点 DontDestroyOnLoad，按预制体维护空闲分类节点与平级 PrefabName[Rented] 节点；租出保持未激活。
    /// </summary>
    public class GameObjectPoolService : IGameObjectPoolService
    {
        // 根节点名；缺省 [GameObjectPool]
        private readonly string _rootName;
        // 池根 Transform（DontDestroyOnLoad）
        private Transform _root;
        // prefab.name → 子池（项目保证名唯一）
        private readonly Dictionary<string, PrefabPool> _poolsByName = new();
        // 已租出 instance.InstanceID → 所属子池
        private readonly Dictionary<int, PrefabPool> _rentedByInstanceId = new();
        // PrefabName → maxSize；仅影响新建子池
        private Dictionary<string, int> _maxSizeByPrefabName;

        /// <summary>
        /// 创建服务并建立根节点。
        /// </summary>
        /// <param name="rootName">Hierarchy 根节点名，如 [GameObjectPool.Core]</param>
        public GameObjectPoolService(string rootName = "[GameObjectPool]")
        {
            _rootName = string.IsNullOrEmpty(rootName) ? "[GameObjectPool]" : rootName;
            EnsureRoot();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        public void Shutdown()
        {
            Clear();
            if (_root != null)
            {
                UnityObject.Destroy(_root.gameObject);
                _root = null;
            }
            _maxSizeByPrefabName = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IGameObjectPoolService:
        public GameObject Rent(GameObject prefab)
        {
            if (prefab == null)
            {
                G.LogError("GameObjectPoolService.Rent prefab is null");
                return null;
            }

            PrefabPool pool = GetOrCreatePool(prefab);
            GameObject instance = pool.Rent();
            _rentedByInstanceId[instance.GetInstanceID()] = pool;
            return instance;
        }

        public void Return(GameObject instance)
        {
            // 只拦真正的 C# null；已 Destroy 的假 null 必须继续往下取 ID，否则租出表泄漏
            if (ReferenceEquals(instance, null))
            {
                G.LogError("GameObjectPoolService.Return instance is null");
                return;
            }

            int instanceId = instance.GetInstanceID(); // Destroy 后托管包装仍在，假 null 上也能取到 ID
            if (instance == null)
            {
                _rentedByInstanceId.Remove(instanceId);
                G.LogError($"GameObjectPoolService.Return instance already destroyed: id={instanceId}");
                return;
            }

            if (!_rentedByInstanceId.TryGetValue(instanceId, out PrefabPool pool))
            {
                G.LogError($"GameObjectPoolService.Return not rented: {instance.name}");
                return;
            }

            if (pool.IsInCategory(instance.transform))
            {
                //IsInCategory == true 只说明instance被改挂到了分类节点（手工改 Hierarchy、错误 SetParent 等）。并不说明对象已在Queue里
                G.LogError($"GameObjectPoolService.Return already pooled: {instance.name}");
            }

            _rentedByInstanceId.Remove(instanceId);
            pool.Return(instance);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null)
            {
                G.LogError("GameObjectPoolService.Prewarm prefab is null");
                return;
            }
            if (count <= 0)
            {
                return;
            }

            GetOrCreatePool(prefab).Prewarm(count);
        }

        public void ApplySettings(IReadOnlyDictionary<string, int> maxSizeByPrefabName)
        {
            if (maxSizeByPrefabName == null)
            {
                _maxSizeByPrefabName = null;
                return;
            }

            _maxSizeByPrefabName = new Dictionary<string, int>(maxSizeByPrefabName.Count);
            foreach (KeyValuePair<string, int> kv in maxSizeByPrefabName)
            {
                if (string.IsNullOrEmpty(kv.Key) || kv.Value < 0)
                {
                    continue;
                }
                _maxSizeByPrefabName[kv.Key] = kv.Value;
            }
        }

        public void Clear(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            Clear(prefab.name);
        }

        public void Clear(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            if (!_poolsByName.TryGetValue(prefabName, out PrefabPool pool))
            {
                return;
            }

            RemovePool(pool);
        }

        public void Clear()
        {
            if (_poolsByName.Count == 0)
            {
                _rentedByInstanceId.Clear();
                return;
            }

            PrefabPool[] snapshot = new PrefabPool[_poolsByName.Count];
            _poolsByName.Values.CopyTo(snapshot, 0);
            for (int i = 0; i < snapshot.Length; i++)
            {
                RemovePool(snapshot[i]);
            }

            _poolsByName.Clear();
            _rentedByInstanceId.Clear();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IGameObjectRentAsync:
        /// <summary>
        /// 经 G.Asset 异步加载 location 对应预制体，再走同步 Rent（未激活，挂该类型 PrefabName[Rented]）；句柄留在 Asset 默认组，池不 Release。
        /// </summary>
        public async UniTask<GameObject> RentAsync(string assetLocation, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(assetLocation))
            {
                G.LogError("GameObjectPoolService.RentAsync assetLocation is null or empty");
                return null;
            }

            if (G.Asset == null)
            {
                G.LogError("GameObjectPoolService.RentAsync G.Asset is null");
                return null;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var handle = G.Asset.LoadAssetAsync<GameObject>(assetLocation, null);
            if (handle == null)
            {
                G.LogError($"GameObjectPoolService.RentAsync load failed: {assetLocation}");
                return null;
            }

            // 等 Provider.Task，避免 HandleBase 当 IEnumerator.ToUniTask 时 yield Provider 每帧 Warning
            // 取消只结束本次等待，不 Release 句柄（Default 组缓存供后续 Rent 共用）
            if (!handle.IsDone)
            {
                var loadTask = handle.Task;
                if (loadTask == null)
                {
                    G.LogError($"GameObjectPoolService.RentAsync load failed: {assetLocation}");
                    return null;
                }

                await loadTask.AsUniTask().AttachExternalCancellation(cancellationToken);
            }

            if (_root == null)
            {
                G.LogError("GameObjectPoolService.RentAsync service already shutdown");
                return null;
            }

            if (handle.Status != EOperationStatus.Succeed)
            {
                G.LogError($"GameObjectPoolService.RentAsync load failed: {assetLocation}");
                return null;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                G.LogError($"GameObjectPoolService.RentAsync asset is not GameObject: {assetLocation}");
                return null;
            }

            return Rent(prefab);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        // 确保根节点存在并挂 DontDestroyOnLoad
        private void EnsureRoot()
        {
            if (_root != null)
            {
                return;
            }

            var go = new GameObject(_rootName);
            UnityObject.DontDestroyOnLoad(go);
            _root = go.transform;
        }

        // 按 prefab.name 取或建子池；capacity 来自 Setting 或 DefaultMaxSize
        private PrefabPool GetOrCreatePool(GameObject prefab)
        {
            EnsureRoot();
            string prefabName = prefab.name;
            if (_poolsByName.TryGetValue(prefabName, out PrefabPool existing))
            {
                return existing;
            }

            int maxSize = IGameObjectPoolService.DefaultMaxSize;
            if (_maxSizeByPrefabName != null && _maxSizeByPrefabName.TryGetValue(prefabName, out int configured))
            {
                maxSize = configured;
            }

            var pool = new PrefabPool(prefab, maxSize, _root, prefabName);
            _poolsByName.Add(prefabName, pool);
            return pool;
        }

        // 销毁子池内实例与二级节点，并清理索引
        private void RemovePool(PrefabPool pool)
        {
            if (pool == null)
            {
                return;
            }

            // 去掉仍指向该子池的租出记录（对象已借出由调用方持有）
            if (_rentedByInstanceId.Count > 0)
            {
                List<int> toRemove = null;
                foreach (KeyValuePair<int, PrefabPool> kv in _rentedByInstanceId)
                {
                    if (kv.Value != pool)
                    {
                        continue;
                    }
                    toRemove ??= new List<int>();
                    toRemove.Add(kv.Key);
                }
                if (toRemove != null)
                {
                    for (int i = 0; i < toRemove.Count; i++)
                    {
                        _rentedByInstanceId.Remove(toRemove[i]);
                    }
                }
            }

            pool.ReleaseInstances(0);
            pool.DestroyCategory();
            _poolsByName.Remove(pool.PrefabName);
        }

        /// <summary>
        /// 单预制体子池：承接 UnityObjectPoolBase 钩子，保证激活/失活与挂载顺序。
        /// </summary>
        private sealed class PrefabPool : UnityObjectPoolBase<GameObject>
        {
            // 空闲分类节点，存放未激活实例
            private Transform _category;
            // 与分类节点平级的租出挂点：PrefabName[Rented]
            private Transform _rentedRoot;
            // 预制体名（主容器键 / Hierarchy / Setting）
            public string PrefabName { get; }

            public PrefabPool(GameObject original, int capacity, Transform poolRoot, string prefabName)
                : base(original, capacity)
            {
                PrefabName = prefabName;

                var categoryGo = new GameObject(prefabName);
                categoryGo.transform.SetParent(poolRoot, false);
                _category = categoryGo.transform;

                string rentedName = prefabName + "[Rented]";
                Transform existingRented = poolRoot.Find(rentedName);
                if (existingRented != null)
                {
                    // Clear 后仍有未改父的租出实例时复用该节点
                    _rentedRoot = existingRented;
                }
                else
                {
                    var rentedGo = new GameObject(rentedName);
                    rentedGo.transform.SetParent(poolRoot, false);
                    _rentedRoot = rentedGo.transform;
                }
            }

            /// <summary>
            /// 预热：直接生成未激活实例入池，不经 Rent 激活路径。
            /// </summary>
            public void Prewarm(int count)
            {
                for (int i = 0; i < count; i++)
                {
                    if (Count >= Capacity)
                    {
                        break;
                    }

                    GameObject instance = UnityObject.Instantiate(m_Original);
                    OnCreate(instance);
                    Return(instance);
                }
            }

            /// <summary>
            /// 是否已挂在本子池二级节点下。
            /// </summary>
            public bool IsInCategory(Transform t)
            {
                return t != null && _category != null && t.parent == _category;
            }

            /// <summary>
            /// 销毁空闲分类节点；PrefabName[Rented] 无子节点才拆，有租出实例则保留以免 Clear 误杀。
            /// </summary>
            public void DestroyCategory()
            {
                if (_category != null)
                {
                    UnityObject.Destroy(_category.gameObject);
                    _category = null;
                }

                if (_rentedRoot == null)
                {
                    return;
                }

                if (_rentedRoot.childCount == 0)
                {
                    UnityObject.Destroy(_rentedRoot.gameObject);
                }
                _rentedRoot = null;
            }

            protected override void OnCreate(GameObject instance)
            {
                instance.SetActive(false);
                instance.transform.SetParent(_category, false);
            }

            protected override void OnRent(GameObject instance)
            {
                // 挂到本类型平级 PrefabName[Rented]，保持未激活；由调用方决定何时 SetActive(true)
                instance.transform.SetParent(_rentedRoot, false);
            }

            protected override void OnReturn(GameObject instance)
            {
                instance.SetActive(false);
                instance.transform.SetParent(_category, false);
            }

            protected override void OnRelease(GameObject instance)
            {
                UnityObject.Destroy(instance);
            }
        }
    }
}
