/*
 * GameObjectPoolService 需求实现概要
 *
 * 1. 核心设计
 *    - 根节点：运行时创建并挂 DontDestroyOnLoad，缺省名 [GameObjectPool]，
 *      构造可指定如 [GameObjectPool.Core] / [GameObjectPool.Battle]。
 *    - 二级节点：根下按 Prefab Name 动态建分类节点，存放该类型未激活实例。
 *    - 子池：每预制体一个 UnityObjectPoolBase<GameObject>（PrefabPool）。
 *
 * 2. 生命周期
 *    - Rent：从二级节点取对象 → SetParent(null, false) 摘下 → SetActive(true)
 *      （顺序不可颠倒，避免多余 OnEnable/OnDisable）。
 *    - Release：先 SetActive(false) → SetParent(category, false) 挂回二级节点。
 *    - 挂载/摘离一律 worldPositionStays=false，避免父节点偏移导致坐标突变。
 *
 * 3. 预热与扩容
 *    - Prewarm：Instantiate 后直接未激活并挂二级节点入池，不经 Rent 激活路径。
 *    - 池空扩容：Instantiate → OnCreate（未激活+挂二级节点）→ OnRent（摘下+激活）。
 *    - 满池（MaxSize）：Release 时 Destroy，不挂回。默认 MaxSize=1000；
 *      ApplySettings(<PrefabName, maxSize>) 仅影响此后新建子池。
 *
 * 4. 标识与边界
 *    - 实例名：保留 Instantiate 默认（PrefabName(Clone)），不额外改名。
 *    - 重复 Release：以租出表为准告警并忽略，防嵌套入池。
 *    - Clear(prefab|name|全部)：Destroy 池内实例并移除二级节点；全部时保留根节点。
 *    - 池键：prefab.name（项目保证预制体名全局唯一）。
 */

using System.Collections.Generic;
using MackySoft.XPool.Unity.ObjectModel;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Xease
{
    /// <summary>
    /// GameObject 池服务实现：根节点 DontDestroyOnLoad，按预制体维护子池与 Hierarchy 二级分类节点。
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

        public GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject instance = Rent(prefab);
            if (instance == null)
            {
                return null;
            }

            Transform transform = instance.transform;
            transform.SetParent(parent, false);
            transform.SetPositionAndRotation(position, rotation);
            return instance;
        }

        public void Release(GameObject instance)
        {
            if (instance == null)
            {
                G.LogError("GameObjectPoolService.Release instance is null");
                return;
            }

            int instanceId = instance.GetInstanceID();
            if (!_rentedByInstanceId.TryGetValue(instanceId, out PrefabPool pool))
            {
                G.LogError($"GameObjectPoolService.Release not rented: {instance.name}");
                return;
            }

            if (pool.IsInCategory(instance.transform))
            {
                G.LogError($"GameObjectPoolService.Release already pooled: {instance.name}");
                _rentedByInstanceId.Remove(instanceId);
                return;
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

            var categoryGo = new GameObject(prefabName);
            categoryGo.transform.SetParent(_root, false);
            var pool = new PrefabPool(prefab, maxSize, categoryGo.transform, prefabName);
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
            // 二级分类节点，存放未激活实例
            private Transform _category;
            // 预制体名（主容器键 / Hierarchy / Setting）
            public string PrefabName { get; }

            public PrefabPool(GameObject original, int capacity, Transform category, string prefabName)
                : base(original, capacity)
            {
                _category = category;
                PrefabName = prefabName;
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
            /// 销毁二级分类节点。
            /// </summary>
            public void DestroyCategory()
            {
                if (_category == null)
                {
                    return;
                }

                UnityObject.Destroy(_category.gameObject);
                _category = null;
            }

            protected override void OnCreate(GameObject instance)
            {
                instance.SetActive(false);
                instance.transform.SetParent(_category, false);
            }

            protected override void OnRent(GameObject instance)
            {
                // 先摘下再激活，避免父节点状态触发多余 OnEnable/OnDisable
                instance.transform.SetParent(null, false);
                instance.SetActive(true);
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
