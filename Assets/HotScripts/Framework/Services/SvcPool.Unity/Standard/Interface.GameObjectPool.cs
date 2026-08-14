using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// Unity GameObject 池服务：按预制体管理实例，根节点挂 DontDestroyOnLoad；每类型空闲节点与平级 PrefabName[Rented] 节点，租出未激活。
    /// </summary>
    public interface IGameObjectPoolService : IService, IGameObjectRentAsync
    {
        // 未在 Setting 中配置时的默认 MaxSize
        const int DefaultMaxSize = 1000;

        /// <summary>
        /// 租用实例；池空时 Instantiate。交出未激活实例，挂到该预制体平级的 PrefabName[Rented]；调用方负责 SetActive(true)。
        /// </summary>
        GameObject Rent(GameObject prefab);

        
        /// <summary>
        /// 归还实例；先失活再挂回二级节点。重复归还或已在池内则忽略。
        /// </summary>
        void Return(GameObject instance);

        /// <summary>
        /// 预热：生成 count 个未激活实例挂入对应二级节点，不超过 MaxSize。
        /// </summary>
        void Prewarm(GameObject prefab, int count);

        /// <summary>
        /// 写入 &lt;PrefabName, maxSize&gt;；仅影响此后新建的子池，已存在子池容量不变。
        /// </summary>
        void ApplySettings(IReadOnlyDictionary<string, int> maxSizeByPrefabName);

        /// <summary>
        /// 清空指定预制体对应子池（Destroy 空闲实例并移除分类节点；无子节点的 PrefabName[Rented] 一并拆掉）。
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


    /// <summary>
    /// 按资源地址异步加载预制体后租用实例；失败返回 null，取消抛 OperationCanceledException。
    /// </summary>
    public interface IGameObjectRentAsync
    {
        /// <summary>
        /// 经 G.Asset 异步加载 location 对应预制体，再走同步 Rent（未激活，挂该类型 PrefabName[Rented]）；句柄留在 Asset 默认组，池不 Release。
        /// </summary>
        /// <param name="assetLocation">YooAsset 资源定位地址</param>
        /// <param name="cancellationToken">仅取消本次等待，不释放句柄；已取消则抛 OperationCanceledException</param>
        /// <returns>租出的实例；location 无效、加载失败或资源非 GameObject 时为 null</returns>
        UniTask<GameObject> RentAsync(string assetLocation, CancellationToken cancellationToken = default);
    }
}
