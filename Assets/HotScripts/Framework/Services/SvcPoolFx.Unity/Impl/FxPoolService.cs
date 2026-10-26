using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using UnityObject = UnityEngine.Object;

namespace Xease
{
    /// <summary>
    /// 特效池。向 <see cref="IGameObjectPoolService"/> 租还实例，并用槽位表跟踪在播特效。
    /// 世界空间实例挂在自身根节点下。到点或挂接父节点已销毁时归还。
    /// 存活上限在租用前判断，超出时返回无实例句柄。
    /// </summary>
    public sealed class FxPoolService : IFxPoolService, IEnvLateUpdate
    {
        // 槽、空闲栈、存活表的初始长度
        private const int InitialCapacity = 64;

        // 到点归还
        private const byte FlagTimed = 1;

        // 寿命用未缩放时间
        private const byte FlagUnscaled = 2;

        // 父节点失效时收回
        private const byte FlagAttached = 4;

        // 租还实例的对象池
        private readonly IGameObjectPoolService _goPool;

        // 世界空间挂点名
        private readonly string _rootName;

        // 世界空间挂点（DontDestroyOnLoad）
        private Transform _root;

        // Shutdown 之后拒绝新的生成
        private bool _shutdown;

        // 槽位；下标即句柄 Slot
        private FxSlot[] _slots;

        // 在播槽下标，交换删除
        private int[] _live;

        // 在播数量
        private int _liveCount;

        // 空闲槽下标栈
        private int[] _free;

        // 空闲栈深度
        private int _freeCount;

        // 下一次发给句柄的创建号。0 留给空句柄
        private int _nextCreateId = 1;

        // 同时存活上限；0 表示不限制
        private int _globalCap;

        // 预制体名 → 上限；null 表示未配置任何预制体上限
        private Dictionary<string, int> _capByName;

        // 预制体 InstanceID → 已解析上限；0 表示该项不限制
        private Dictionary<int, int> _resolvedCap;

        // 预制体 InstanceID → 当前真实存活数；只记录设了上限的预制体
        private Dictionary<int, int> _liveByPrefab;

        // 本帧要收回的槽，与创建号成对；槽被复用后创建号对不上就跳过
        private int[] _dying;
        private int[] _dyingIds;

        /// <summary>
        /// 创建特效池，并建立世界空间挂点。
        /// </summary>
        /// <param name="gameObjectPool">租还实例用的对象池</param>
        /// <param name="rootName">Hierarchy 挂点名</param>
        public FxPoolService(IGameObjectPoolService gameObjectPool, string rootName = "[FxPool]")
        {
            _goPool = gameObjectPool;
            _rootName = string.IsNullOrEmpty(rootName) ? "[FxPool]" : rootName;
            if (_goPool == null)
            {
                G.LogError("FxPoolService GameObjectPool is null");
            }

            Allocate(InitialCapacity);
            EnsureRoot();
        }

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        /// <summary>
        /// 归还全部在播特效并拆掉挂点。GameObject 池里的闲置实例保留。
        /// </summary>
        public void Shutdown()
        {
            _shutdown = true;
            StopAll();
            if (_root != null)
            {
                UnityObject.Destroy(_root.gameObject);
                _root = null;
            }

            _capByName = null;
            _resolvedCap = null;
            _liveByPrefab = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// IEnvLateUpdate:
        /// <summary>
        /// 先扣剩余时间并记下要收回的槽，再摘离并归还。
        /// </summary>
        public void EnvLateUpdate(float dt, float dt_unscaled)
        {
            if (_shutdown || _liveCount == 0)
            {
                return;
            }

            int dyingCount = 0;
            for (int i = 0; i < _liveCount; i++)
            {
                int slotIndex = _live[i];
                Transform trans = _slots[slotIndex].Trans;
                byte flags = _slots[slotIndex].Flags;
                bool parentLost = (flags & FlagAttached) != 0 && _slots[slotIndex].Parent == null;
                if (trans == null || parentLost)
                {
                    _dying[dyingCount] = slotIndex;
                    _dyingIds[dyingCount] = _slots[slotIndex].CreateId;
                    dyingCount++;
                    continue;
                }

                if ((flags & FlagTimed) == 0)
                {
                    continue;
                }

                float step = (flags & FlagUnscaled) != 0 ? dt_unscaled : dt;
                _slots[slotIndex].Remain -= step;
                if (_slots[slotIndex].Remain <= 0f)
                {
                    _dying[dyingCount] = slotIndex;
                    _dyingIds[dyingCount] = _slots[slotIndex].CreateId;
                    dyingCount++;
                }
            }

            for (int i = 0; i < dyingCount; i++)
            {
                int slotIndex = _dying[i];
                if (_slots[slotIndex].CreateId != _dyingIds[i])
                {
                    continue;
                }

                Transform trans = _slots[slotIndex].Trans;
                if (trans != null && (_slots[slotIndex].Flags & FlagAttached) != 0 && _slots[slotIndex].Parent == null)
                {
                    // Unity 假 null：本帧已 Destroy。先摘离，避免帧末把池实例一起拆掉
                    trans.SetParent(_root, true);
                }

                StopSlot(slotIndex);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// IFxPoolService:
        /// <summary>
        /// 在世界坐标生成特效。位姿写完再激活。
        /// </summary>
        public FxHandle Spawn(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation, float lifetimeSec, bool unscaledTime = false)
        {
            return SpawnInternal(prefab, null, worldPosition, worldRotation, false, lifetimeSec, unscaledTime);
        }

        /// <summary>
        /// 在世界坐标生成特效，旋转为单位四元数。
        /// </summary>
        public FxHandle Spawn(GameObject prefab, Vector3 worldPosition, float lifetimeSec, bool unscaledTime = false)
        {
            return Spawn(prefab, worldPosition, Quaternion.identity, lifetimeSec, unscaledTime);
        }

        /// <summary>
        /// 挂到父节点下，按本地坐标生成。
        /// </summary>
        public FxHandle Spawn(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, float lifetimeSec, bool unscaledTime = false)
        {
            return SpawnInternal(prefab, parent, localPosition, localRotation, true, lifetimeSec, unscaledTime);
        }

        /// <summary>
        /// 按资源地址加载预制体后，在世界坐标生成。地址已完成时不分配异步状态机。
        /// </summary>
        public UniTask<FxHandle> SpawnAsync(string assetLocation, Vector3 worldPosition, Quaternion worldRotation, float lifetimeSec, bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            return SpawnAsyncInternal(assetLocation, null, worldPosition, worldRotation, false, lifetimeSec, unscaledTime, cancellationToken);
        }

        /// <summary>
        /// 按资源地址加载预制体后，在世界坐标生成，旋转为单位四元数。
        /// </summary>
        public UniTask<FxHandle> SpawnAsync(string assetLocation, Vector3 worldPosition, float lifetimeSec, bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            return SpawnAsync(assetLocation, worldPosition, Quaternion.identity, lifetimeSec, unscaledTime, cancellationToken);
        }

        /// <summary>
        /// 按资源地址加载预制体后，挂到父节点并按本地坐标生成。
        /// </summary>
        public UniTask<FxHandle> SpawnAsync(string assetLocation, Transform parent, Vector3 localPosition, Quaternion localRotation, float lifetimeSec, bool unscaledTime = false, CancellationToken cancellationToken = default)
        {
            return SpawnAsyncInternal(assetLocation, parent, localPosition, localRotation, true, lifetimeSec, unscaledTime, cancellationToken);
        }

        /// <summary>
        /// 槽位仍对应该创建号，且实例还在。
        /// </summary>
        public bool IsValid(FxHandle handle)
        {
            return TryMatch(handle, out int slotIndex) && _slots[slotIndex].Trans != null;
        }

        /// <summary>
        /// 归还到对象池。
        /// </summary>
        public void Stop(FxHandle handle)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            StopSlot(slotIndex);
        }

        /// <summary>
        /// 设置世界坐标。
        /// </summary>
        public void SetWorldPosition(FxHandle handle, Vector3 position)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            _slots[slotIndex].Trans.position = position;
        }

        /// <summary>
        /// 设置相对当前父节点的本地坐标。
        /// </summary>
        public void SetLocalPosition(FxHandle handle, Vector3 localPosition)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            _slots[slotIndex].Trans.localPosition = localPosition;
        }

        /// <summary>
        /// 设置世界旋转。
        /// </summary>
        public void SetWorldRotation(FxHandle handle, Quaternion rotation)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            _slots[slotIndex].Trans.rotation = rotation;
        }

        /// <summary>
        /// 设置相对当前父节点的本地旋转。
        /// </summary>
        public void SetLocalRotation(FxHandle handle, Quaternion localRotation)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            _slots[slotIndex].Trans.localRotation = localRotation;
        }

        /// <summary>
        /// 设置根节点本地缩放。
        /// </summary>
        public void SetLocalScale(FxHandle handle, Vector3 scale)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            _slots[slotIndex].Trans.localScale = scale;
        }

        /// <summary>
        /// 挂到父节点下。父节点被销毁时，这条特效会在帧末被收回。
        /// </summary>
        public void Attach(FxHandle handle, Transform parent, bool worldPositionStays = true)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            if (parent == null)
            {
                G.LogError("FxPoolService.Attach parent is null");
                return;
            }

            _slots[slotIndex].Trans.SetParent(parent, worldPositionStays);
            _slots[slotIndex].Parent = parent;
            _slots[slotIndex].Flags = (byte)(_slots[slotIndex].Flags | FlagAttached);
        }

        /// <summary>
        /// 从父节点摘下，挂回特效池根节点并保持世界坐标。
        /// </summary>
        public void Detach(FxHandle handle)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            _slots[slotIndex].Trans.SetParent(_root, true);
            _slots[slotIndex].Parent = null;
            _slots[slotIndex].Flags = (byte)(_slots[slotIndex].Flags & ~FlagAttached);
        }

        /// <summary>
        /// 从现在起重设自动回收时间。小于等于 0 则改为一直播放到 Stop 或父节点失效。
        /// </summary>
        public void ResetLifetime(FxHandle handle, float lifetimeSec)
        {
            if (!TryGet(handle, out int slotIndex))
            {
                return;
            }

            if (lifetimeSec > 0f)
            {
                _slots[slotIndex].Flags = (byte)(_slots[slotIndex].Flags | FlagTimed);
                _slots[slotIndex].Remain = lifetimeSec;
                return;
            }

            _slots[slotIndex].Flags = (byte)(_slots[slotIndex].Flags & ~FlagTimed);
            _slots[slotIndex].Remain = 0f;
        }

        /// <summary>
        /// 同时存活的真实实例上限。小于等于 0 表示不限制。只拒绝此后的新实例。
        /// </summary>
        public void SetGlobalCap(int maxLive)
        {
            _globalCap = maxLive > 0 ? maxLive : 0;
        }

        /// <summary>
        /// 按预制体名设置同时存活上限。小于等于 0 表示去掉该项。
        /// </summary>
        public void SetPrefabCap(string prefabName, int maxLive)
        {
            if (string.IsNullOrEmpty(prefabName))
            {
                return;
            }

            if (maxLive <= 0)
            {
                if (_capByName == null)
                {
                    return;
                }

                _capByName.Remove(prefabName);
                if (_capByName.Count == 0)
                {
                    _capByName = null;
                }
            }
            else
            {
                if (_capByName == null)
                {
                    _capByName = new Dictionary<string, int>();
                }

                _capByName[prefabName] = maxLive;
            }

            if (_resolvedCap == null)
            {
                _resolvedCap = new Dictionary<int, int>();
            }

            if (_liveByPrefab == null)
            {
                _liveByPrefab = new Dictionary<int, int>();
            }

            // 名字到 InstanceID 的解析作废；存活数按当前槽位重算，避免中途改上限后漏计
            _resolvedCap.Clear();
            RebuildLiveCounts();
        }

        /// <summary>
        /// 预热，转发到 GameObject 池。
        /// </summary>
        public void Prewarm(GameObject prefab, int count)
        {
            if (_shutdown || _goPool == null)
            {
                return;
            }

            _goPool.Prewarm(prefab, count);
        }

        /// <summary>
        /// 异步预热已加载的预制体，转发到 GameObject 池。
        /// </summary>
        public UniTask PrewarmAsync(GameObject prefab, int count, CancellationToken cancellationToken = default)
        {
            if (_shutdown || _goPool == null)
            {
                return UniTask.CompletedTask;
            }

            return _goPool.PrewarmAsync(prefab, count, cancellationToken);
        }

        /// <summary>
        /// 按资源地址异步预热，转发到 GameObject 池。
        /// </summary>
        public UniTask PrewarmAsync(string assetLocation, int count, CancellationToken cancellationToken = default)
        {
            if (_shutdown || _goPool == null)
            {
                return UniTask.CompletedTask;
            }

            return _goPool.PrewarmAsync(assetLocation, count, cancellationToken);
        }

        /// <summary>
        /// 归还全部在播特效。GameObject 池里的闲置实例保留。
        /// </summary>
        public void StopAll()
        {
            // 每次从末尾归还；次数用尽仍留在表里则停下，避免空转
            int guard = _liveCount;
            while (_liveCount > 0 && guard-- > 0)
            {
                StopSlot(_live[_liveCount - 1]);
            }
        }

        /// <summary>
        /// 归还该预制体资产当前在播的实例。
        /// </summary>
        public void StopByPrefab(GameObject prefab)
        {
            if (prefab == null)
            {
                return;
            }

            int prefabId = prefab.GetInstanceID();
            int index = 0;
            while (index < _liveCount)
            {
                int slotIndex = _live[index];
                if (_slots[slotIndex].PrefabId != prefabId)
                {
                    index++;
                    continue;
                }

                StopSlot(slotIndex);
                if (index < _liveCount && _live[index] == slotIndex)
                {
                    index++;
                }
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>
        /// 一条在播特效。CreateId 与句柄一致才有效。
        /// </summary>
        private struct FxSlot
        {
            // 创建号；0 表示空槽
            public int CreateId;

            // 在存活表中的下标
            public int LiveIndex;

            // 预制体 InstanceID，供上限计数和按预制体收回
            public int PrefabId;

            // 剩余寿命（秒）；未设限时不读取
            public float Remain;

            // FlagTimed / FlagUnscaled / FlagAttached
            public byte Flags;

            // 实例变换；外部 Destroy 后为假 null
            public Transform Trans;

            // 调用方挂上的父节点；未挂接时为空
            public Transform Parent;

            // 实例上的播放缓存
            public FxPlayback Playback;
        }

        // 先写位姿再激活。激活后清粒子并播放，LOD 的 OnEnable 会先于播放执行。
        private FxHandle SpawnInternal(GameObject prefab, Transform parent, Vector3 pos, Quaternion rot, bool localSpace, float lifetimeSec, bool unscaledTime)
        {
            if (_shutdown || _root == null)
            {
                G.LogError("FxPoolService.Spawn service already shutdown");
                return default;
            }

            if (_goPool == null)
            {
                G.LogError("FxPoolService.Spawn GameObjectPool is null");
                return default;
            }

            if (prefab == null)
            {
                G.LogError("FxPoolService.Spawn prefab is null");
                return default;
            }

            if (localSpace && parent == null)
            {
                G.LogError("FxPoolService.Spawn parent is null");
                return default;
            }

            int prefabId = prefab.GetInstanceID();
            if (_globalCap > 0 && _liveCount >= _globalCap)
            {
                return CulledHandle();
            }

            if (OverPrefabCap(prefab, prefabId))
            {
                return CulledHandle();
            }

            GameObject instance = _goPool.Rent(prefab);
            if (instance == null)
            {
                G.LogError("FxPoolService.Spawn Rent returned null");
                return default;
            }

            Transform trans = instance.transform;
            if (localSpace)
            {
                trans.SetParent(parent, false);
                trans.localPosition = pos;
                trans.localRotation = rot;
            }
            else
            {
                trans.SetParent(_root, false);
                trans.SetPositionAndRotation(pos, rot);
            }

            // 写回预制体根缩放，清掉上一轮对根节点的缩放
            trans.localScale = prefab.transform.localScale;
            FxPlayback playback = GetOrAddPlayback(instance);
            playback.Prepare();
            instance.SetActive(true);
            playback.Play();

            int slotIndex = RentSlot();
            byte flags = 0;
            if (lifetimeSec > 0f)
            {
                flags |= FlagTimed;
            }

            if (unscaledTime)
            {
                flags |= FlagUnscaled;
            }

            if (localSpace)
            {
                flags |= FlagAttached;
            }

            int createId = NextCreateId();
            _slots[slotIndex].CreateId = createId;
            _slots[slotIndex].PrefabId = prefabId;
            _slots[slotIndex].Remain = lifetimeSec;
            _slots[slotIndex].Flags = flags;
            _slots[slotIndex].Trans = trans;
            _slots[slotIndex].Parent = localSpace ? parent : null;
            _slots[slotIndex].Playback = playback;

            int liveIndex = _liveCount;
            _live[_liveCount++] = slotIndex;
            _slots[slotIndex].LiveIndex = liveIndex;
            NoteLivePrefab(prefabId);
            return new FxHandle(createId, slotIndex);
        }

        // 地址已完成时直接 Spawn，不进入异步状态机
        private UniTask<FxHandle> SpawnAsyncInternal(string assetLocation, Transform parent, Vector3 pos, Quaternion rot, bool localSpace, float lifetimeSec, bool unscaledTime, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return UniTask.FromCanceled<FxHandle>(cancellationToken);
            }

            if (_shutdown || _root == null)
            {
                G.LogError("FxPoolService.SpawnAsync service already shutdown");
                return UniTask.FromResult(default(FxHandle));
            }

            if (localSpace && parent == null)
            {
                G.LogError("FxPoolService.SpawnAsync parent is null");
                return UniTask.FromResult(default(FxHandle));
            }

            AssetHandle handle = BeginLoadPrefab(assetLocation, "SpawnAsync");
            if (handle == null)
            {
                return UniTask.FromResult(default(FxHandle));
            }

            if (handle.IsDone)
            {
                GameObject prefab = ResolvePrefab(handle, assetLocation, "SpawnAsync");
                if (prefab == null)
                {
                    return UniTask.FromResult(default(FxHandle));
                }

                return UniTask.FromResult(SpawnInternal(prefab, parent, pos, rot, localSpace, lifetimeSec, unscaledTime));
            }

            return WaitAndSpawnAsync(handle, assetLocation, cancellationToken, parent, pos, rot, localSpace, lifetimeSec, unscaledTime);
        }

        // 仅未完成的句柄走这里
        private async UniTask<FxHandle> WaitAndSpawnAsync(AssetHandle handle, string assetLocation, CancellationToken cancellationToken, Transform parent, Vector3 pos, Quaternion rot, bool localSpace, float lifetimeSec, bool unscaledTime)
        {
            GameObject prefab = await WaitPrefabAsync(handle, assetLocation, cancellationToken, "SpawnAsync");
            if (prefab == null)
            {
                return default;
            }

            return SpawnInternal(prefab, parent, pos, rot, localSpace, lifetimeSec, unscaledTime);
        }

        // 启动或取出 location 句柄；失败返回 null 并打日志。不 Release 句柄。
        private AssetHandle BeginLoadPrefab(string assetLocation, string logOp)
        {
            if (string.IsNullOrEmpty(assetLocation))
            {
                G.LogError($"FxPoolService.{logOp} assetLocation is null or empty");
                return null;
            }

            if (G.Asset == null)
            {
                G.LogError($"FxPoolService.{logOp} G.Asset is null");
                return null;
            }

            AssetHandle handle = G.Asset.LoadAssetAsync<GameObject>(assetLocation, null);
            if (handle == null)
            {
                G.LogError($"FxPoolService.{logOp} load failed: {assetLocation}");
            }

            return handle;
        }

        // 句柄已完成后校验预制体；失败返回 null。
        private GameObject ResolvePrefab(AssetHandle handle, string assetLocation, string logOp)
        {
            if (_root == null)
            {
                G.LogError($"FxPoolService.{logOp} service already shutdown");
                return null;
            }

            if (handle == null || handle.Status != EOperationStatus.Succeed)
            {
                G.LogError($"FxPoolService.{logOp} load failed: {assetLocation}");
                return null;
            }

            var prefab = handle.AssetObject as GameObject;
            if (prefab == null)
            {
                G.LogError($"FxPoolService.{logOp} asset is not GameObject: {assetLocation}");
                return null;
            }

            return prefab;
        }

        // 等 Provider.Task；取消只结束本次等待，不 Release 句柄
        private async UniTask<GameObject> WaitPrefabAsync(AssetHandle handle, string assetLocation, CancellationToken cancellationToken, string logOp)
        {
            var loadTask = handle.Task;
            if (loadTask == null)
            {
                G.LogError($"FxPoolService.{logOp} load failed: {assetLocation}");
                return null;
            }

            // 避免 HandleBase 当 IEnumerator.ToUniTask 时 yield Provider 每帧 Warning
            await loadTask.AsUniTask().AttachExternalCancellation(cancellationToken);
            return ResolvePrefab(handle, assetLocation, logOp);
        }

        // 超出上限：发创建号，不占槽，不租实例
        private FxHandle CulledHandle()
        {
            return new FxHandle(NextCreateId(), FxHandle.NoSlot);
        }

        private int NextCreateId()
        {
            int id = _nextCreateId;
            _nextCreateId++;
            if (_nextCreateId <= 0)
            {
                _nextCreateId = 1;
            }

            return id;
        }

        // 未配置任何预制体上限时直接放过。已满则拒绝，不回收旧实例。
        private bool OverPrefabCap(GameObject prefab, int prefabId)
        {
            if (_capByName == null || _resolvedCap == null)
            {
                return false;
            }

            if (!_resolvedCap.TryGetValue(prefabId, out int cap))
            {
                if (!_capByName.TryGetValue(prefab.name, out cap))
                {
                    cap = 0;
                }

                _resolvedCap[prefabId] = cap;
            }

            if (cap <= 0)
            {
                return false;
            }

            int live = 0;
            if (_liveByPrefab != null)
            {
                _liveByPrefab.TryGetValue(prefabId, out live);
            }

            return live >= cap;
        }

        // 按槽里的预制体 InstanceID 重算存活数，供中途修改上限后继续拒绝新实例
        private void RebuildLiveCounts()
        {
            if (_liveByPrefab == null)
            {
                return;
            }

            _liveByPrefab.Clear();
            for (int i = 0; i < _liveCount; i++)
            {
                int prefabId = _slots[_live[i]].PrefabId;
                _liveByPrefab.TryGetValue(prefabId, out int live);
                _liveByPrefab[prefabId] = live + 1;
            }
        }

        private void NoteLivePrefab(int prefabId)
        {
            if (_capByName == null || _resolvedCap == null || _liveByPrefab == null)
            {
                return;
            }

            if (!_resolvedCap.TryGetValue(prefabId, out int cap) || cap <= 0)
            {
                return;
            }

            _liveByPrefab.TryGetValue(prefabId, out int live);
            _liveByPrefab[prefabId] = live + 1;
        }

        private void UnnoteLivePrefab(int prefabId)
        {
            if (_capByName == null || _liveByPrefab == null)
            {
                return;
            }

            if (!_liveByPrefab.TryGetValue(prefabId, out int live))
            {
                return;
            }

            if (live <= 1)
            {
                _liveByPrefab.Remove(prefabId);
                return;
            }

            _liveByPrefab[prefabId] = live - 1;
        }

        private bool TryMatch(FxHandle handle, out int slotIndex)
        {
            slotIndex = handle.Slot;
            if (_shutdown || handle.CreateId == 0)
            {
                return false;
            }

            if ((uint)slotIndex >= (uint)_slots.Length)
            {
                return false;
            }

            return _slots[slotIndex].CreateId == handle.CreateId;
        }

        // 实例已被外部销毁时清槽并视为无效
        private bool TryGet(FxHandle handle, out int slotIndex)
        {
            if (!TryMatch(handle, out slotIndex))
            {
                return false;
            }

            if (_slots[slotIndex].Trans != null)
            {
                return true;
            }

            // 外部 Destroy 后只清槽，避免再 Return
            StopSlot(slotIndex);
            return false;
        }

        // 先作废创建号，再归还。归还触发的 OnDisable 里再次 Stop 会直接返回。
        private void StopSlot(int slotIndex)
        {
            if ((uint)slotIndex >= (uint)_slots.Length || _slots[slotIndex].CreateId == 0)
            {
                return;
            }

            Transform trans = _slots[slotIndex].Trans;
            int prefabId = _slots[slotIndex].PrefabId;
            FxPlayback playback = _slots[slotIndex].Playback;
            _slots[slotIndex].CreateId = 0;

            if (trans != null)
            {
                if (playback != null)
                {
                    playback.StopAndClear();
                }

                if (_goPool != null)
                {
                    _goPool.Return(trans.gameObject);
                }
            }

            int liveIndex = _slots[slotIndex].LiveIndex;
            if ((uint)liveIndex >= (uint)_liveCount || _live[liveIndex] != slotIndex)
            {
                liveIndex = IndexOfLive(slotIndex);
            }

            UnnoteLivePrefab(prefabId);
            if (liveIndex >= 0)
            {
                RemoveLiveAt(liveIndex);
            }

            FreeSlot(slotIndex);
        }

        private int IndexOfLive(int slotIndex)
        {
            for (int i = 0; i < _liveCount; i++)
            {
                if (_live[i] == slotIndex)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RemoveLiveAt(int liveIndex)
        {
            int last = _liveCount - 1;
            int movedSlot = _live[last];
            _live[liveIndex] = movedSlot;
            _slots[movedSlot].LiveIndex = liveIndex;
            _liveCount = last;
        }

        private void FreeSlot(int slotIndex)
        {
            _slots[slotIndex].CreateId = 0;
            _slots[slotIndex].LiveIndex = -1;
            _slots[slotIndex].PrefabId = 0;
            _slots[slotIndex].Remain = 0f;
            _slots[slotIndex].Flags = 0;
            _slots[slotIndex].Trans = null;
            _slots[slotIndex].Parent = null;
            _slots[slotIndex].Playback = null;
            _free[_freeCount++] = slotIndex;
        }

        private int RentSlot()
        {
            if (_freeCount == 0)
            {
                Grow();
            }

            _freeCount--;
            return _free[_freeCount];
        }

        private void Allocate(int capacity)
        {
            _slots = new FxSlot[capacity];
            _live = new int[capacity];
            _dying = new int[capacity];
            _dyingIds = new int[capacity];
            _free = new int[capacity];
            _freeCount = 0;
            _liveCount = 0;
            for (int i = capacity - 1; i >= 0; i--)
            {
                _free[_freeCount++] = i;
            }
        }

        // 空闲栈耗尽时加长槽、存活表和空闲栈，并把新下标压栈
        private void Grow()
        {
            int oldCapacity = _slots.Length;
            int newCapacity = oldCapacity << 1;
            Array.Resize(ref _slots, newCapacity);
            Array.Resize(ref _live, newCapacity);
            Array.Resize(ref _dying, newCapacity);
            Array.Resize(ref _dyingIds, newCapacity);
            Array.Resize(ref _free, newCapacity);
            for (int i = newCapacity - 1; i >= oldCapacity; i--)
            {
                _free[_freeCount++] = i;
            }
        }

        private static FxPlayback GetOrAddPlayback(GameObject instance)
        {
            FxPlayback playback = instance.GetComponent<FxPlayback>();
            if (playback == null)
            {
                playback = instance.AddComponent<FxPlayback>();
            }

            return playback;
        }

        // 世界空间特效的挂点
        private void EnsureRoot()
        {
            if (_shutdown || _root != null)
            {
                return;
            }

            var go = new GameObject(_rootName);
            UnityObject.DontDestroyOnLoad(go);
            _root = go.transform;
        }
    }
}
