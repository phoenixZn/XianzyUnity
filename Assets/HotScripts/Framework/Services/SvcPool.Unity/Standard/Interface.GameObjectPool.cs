using System.Collections.Generic;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// Unity GameObject 池服务：按预制体管理实例，根节点挂 DontDestroyOnLoad，二级节点按 prefab 名分类。
    /// </summary>
    public interface IGameObjectPoolService : IService
    {
        // 未在 Setting 中配置时的默认 MaxSize
        const int DefaultMaxSize = 1000;

        /// <summary>
        /// 租用实例；池空时 Instantiate，并按「摘下父节点 → SetActive(true)」激活。
        /// </summary>
        GameObject Rent(GameObject prefab);

        /// <summary>
        /// 租用并放置到指定位姿；parent 可为 null（留在场景根）。
        /// </summary>
        GameObject Rent(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null);

        /// <summary>
        /// 归还实例；先失活再挂回二级节点。重复归还或已在池内则忽略。
        /// </summary>
        void Release(GameObject instance);

        /// <summary>
        /// 预热：生成 count 个未激活实例挂入对应二级节点，不超过 MaxSize。
        /// </summary>
        void Prewarm(GameObject prefab, int count);

        /// <summary>
        /// 写入 &lt;PrefabName, maxSize&gt;；仅影响此后新建的子池，已存在子池容量不变。
        /// </summary>
        void ApplySettings(IReadOnlyDictionary<string, int> maxSizeByPrefabName);

        /// <summary>
        /// 清空指定预制体对应子池（Destroy 池内实例并移除二级节点）。
        /// </summary>
        void Clear(GameObject prefab);

        /// <summary>
        /// 按预制体名清空子池（与 Setting 键一致）。
        /// </summary>
        void Clear(string prefabName);

        /// <summary>
        /// 清空全部子池，保留根节点。
        /// </summary>
        void Clear();
    }
}
