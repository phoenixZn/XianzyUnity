using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xease.CoreGame
{
    /// <summary>
    /// 每帧一次的受击目标快照：把带 HitBodyComponent 与 TransformComponent 的存活实体抄进扁平数组，
    /// 供各套路做扫掠查询与追踪定位。当前查询为暴力遍历；以后可在内部换成空间划分，调用方不变。
    /// 由 UniSubobjectsComponent 持有，仅主线程使用。
    /// </summary>
    public sealed class SubobjectTargetSnapshot
    {
        // 首次分配的目标与查询结果容量，不足时倍增
        private const int InitialCapacity = 64;
        // 线段长度平方低于此值时按点处理
        private const float SegmentSqrEpsilon = 1e-12f;

        private LogicEntity[] _entities = new LogicEntity[InitialCapacity];
        private int[] _creationIndices = new int[InitialCapacity];
        private Vector3[] _positions = new Vector3[InitialCapacity];
        private float[] _radii = new float[InitialCapacity];
        private uint[] _layers = new uint[InitialCapacity];
        private EFaction[] _factions = new EFaction[InitialCapacity];
        // creationIndex 到快照下标，供追踪查目标
        private readonly Dictionary<int, int> _indexByCreation = new(InitialCapacity);

        // 最近一次 Query 的结果：目标下标与沿线段的参数，按参数升序
        private int[] _resultIndices = new int[InitialCapacity];
        private float[] _resultParams = new float[InitialCapacity];

        public int Count { get; private set; } // 本帧目标数

        //////////////////////////////////////////////////////////////////////////
        /// This：构建

        /// <summary>
        /// 用 entities 重建快照，跳过已禁用、已挂死亡组件或缺少受击体、变换的实体。
        /// </summary>
        public void Rebuild(List<LogicEntity> entities)
        {
            Clear();
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                if (!entity.isEnabled || entity.hasComDeath || !entity.hasComHitBody || !entity.hasComTransform)
                    continue;

                EnsureCapacity(Count + 1);
                var body = entity.comHitBody;
                var index = Count++;
                _entities[index] = entity;
                _creationIndices[index] = entity.creationIndex;
                _positions[index] = entity.position;
                _radii[index] = body.Radius;
                _layers[index] = body.Layer;
                _factions[index] = entity.Faction;
                _indexByCreation[entity.creationIndex] = index;
            }
        }

        /// <summary>
        /// 清空快照，并释放对实体的引用。
        /// </summary>
        public void Clear()
        {
            Array.Clear(_entities, 0, Count);
            _indexByCreation.Clear();
            Count = 0;
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：查询

        /// <summary>
        /// 查目标位置；creationIndex 不在本帧快照中时返回 false。
        /// </summary>
        public bool TryGetPosition(int creationIndex, out Vector3 position)
        {
            if (_indexByCreation.TryGetValue(creationIndex, out var index))
            {
                position = _positions[index];
                return true;
            }
            position = default;
            return false;
        }

        /// <summary>
        /// 找出与线段 from→to 的距离不超过 radius 加目标半径、且层与 layerMask 相交的目标，返回个数。
        /// 结果按沿线段的先后排序，用 ResultIndex / ResultParam 读取，在下一次 Query 前有效。
        /// </summary>
        public int Query(Vector3 from, Vector3 to, float radius, uint layerMask)
        {
            var segment = to - from;
            var lengthSqr = segment.sqrMagnitude;
            var resultCount = 0;
            for (var i = 0; i < Count; i++)
            {
                if ((_layers[i] & layerMask) == 0)
                    continue;

                var center = _positions[i];
                var t = 0f;
                if (lengthSqr > SegmentSqrEpsilon)
                    t = Mathf.Clamp01(Vector3.Dot(center - from, segment) / lengthSqr);
                var closest = from + segment * t;
                var reach = radius + _radii[i];
                if ((center - closest).sqrMagnitude > reach * reach)
                    continue;

                InsertResult(ref resultCount, i, t);
            }
            return resultCount;
        }

        /// <summary>
        /// 第 n 个查询结果的目标下标。
        /// </summary>
        public int ResultIndex(int n) => _resultIndices[n];

        /// <summary>
        /// 第 n 个查询结果在线段上的参数，0 为起点、1 为终点。
        /// </summary>
        public float ResultParam(int n) => _resultParams[n];

        /// <summary>
        /// 目标实体；只在本帧快照有效期内使用。
        /// </summary>
        public LogicEntity EntityAt(int index) => _entities[index];

        /// <summary>
        /// 目标的 creationIndex。
        /// </summary>
        public int CreationIndexAt(int index) => _creationIndices[index];

        /// <summary>
        /// 目标在快照时刻的位置。
        /// </summary>
        public Vector3 PositionAt(int index) => _positions[index];

        /// <summary>
        /// 目标受击半径。
        /// </summary>
        public float RadiusAt(int index) => _radii[index];

        /// <summary>
        /// 目标阵营；未设置阵营时为 EFaction.Invalid。
        /// </summary>
        public EFaction FactionAt(int index) => _factions[index];

        // 插入排序：同一线段命中的目标通常很少
        private void InsertResult(ref int resultCount, int targetIndex, float t)
        {
            if (resultCount == _resultIndices.Length)
            {
                Array.Resize(ref _resultIndices, resultCount * 2);
                Array.Resize(ref _resultParams, resultCount * 2);
            }

            var slot = resultCount++;
            while (slot > 0 && _resultParams[slot - 1] > t)
            {
                _resultIndices[slot] = _resultIndices[slot - 1];
                _resultParams[slot] = _resultParams[slot - 1];
                slot--;
            }
            _resultIndices[slot] = targetIndex;
            _resultParams[slot] = t;
        }

        private void EnsureCapacity(int required)
        {
            if (required <= _entities.Length)
                return;

            var capacity = _entities.Length * 2;
            while (capacity < required)
                capacity *= 2;
            Array.Resize(ref _entities, capacity);
            Array.Resize(ref _creationIndices, capacity);
            Array.Resize(ref _positions, capacity);
            Array.Resize(ref _radii, capacity);
            Array.Resize(ref _layers, capacity);
            Array.Resize(ref _factions, capacity);
        }
    }
}
