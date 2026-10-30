using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 特效池。实例向 GameObject 池租还，调用方只持有 <see cref="FxHandle"/>。
    /// 负责限时回收、父节点销毁后的救回，以及可选的存活上限。
    /// 无效句柄上的操作直接返回。逐帧推进由实现类的 IEnvLateUpdate 承担。
    /// </summary>
    public interface IFxPoolService : IService
    {
        /// <summary>
        /// 在世界坐标生成特效。位姿写完再激活。
        /// </summary>
        /// <param name="prefab">预制体</param>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="worldRotation">世界旋转</param>
        /// <param name="lifetimeSec">大于 0 时到点归还；否则保持到 Stop 或父节点失效</param>
        /// <param name="unscaledTime">为 true 时寿命不跟随 timeScale</param>
        /// <returns>空句柄、超限后的无实例句柄，或在播句柄</returns>
        FxHandle Spawn(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation, float lifetimeSec, bool unscaledTime = false);

        /// <summary>
        /// 在世界坐标生成特效，旋转为单位四元数。
        /// </summary>
        /// <param name="lifetimeSec">大于 0 时到点归还；否则保持到 Stop 或父节点失效</param>
        /// <param name="unscaledTime">为 true 时寿命不跟随 timeScale</param>
        /// <returns>空句柄、超限后的无实例句柄，或在播句柄</returns>
        FxHandle Spawn(GameObject prefab, Vector3 worldPosition, float lifetimeSec, bool unscaledTime = false);

        /// <summary>
        /// 挂到父节点下，按本地坐标生成。激活前写完本地位姿。
        /// </summary>
        /// <param name="parent">挂点；为空则返回空句柄</param>
        /// <param name="localPosition">相对父节点的坐标</param>
        /// <param name="localRotation">相对父节点的旋转</param>
        /// <param name="lifetimeSec">大于 0 时到点归还；否则保持到 Stop 或父节点失效</param>
        /// <param name="unscaledTime">为 true 时寿命不跟随 timeScale</param>
        /// <returns>空句柄、超限后的无实例句柄，或在播句柄</returns>
        FxHandle Spawn(GameObject prefab, Transform parent, Vector3 localPosition, Quaternion localRotation, float lifetimeSec, bool unscaledTime = false);

        /// <summary>
        /// 按资源地址加载预制体后，在世界坐标生成。地址已完成时不分配异步状态机。
        /// </summary>
        /// <param name="assetLocation">YooAsset 资源定位地址</param>
        /// <param name="lifetimeSec">大于 0 时到点归还；否则保持到 Stop 或父节点失效</param>
        /// <param name="unscaledTime">为 true 时寿命不跟随 timeScale</param>
        /// <param name="cancellationToken">只取消本次等待，不释放资源句柄；已取消则抛 OperationCanceledException</param>
        /// <returns>空句柄、超限后的无实例句柄，或在播句柄</returns>
        UniTask<FxHandle> SpawnAsync(string assetLocation, Vector3 worldPosition, Quaternion worldRotation, float lifetimeSec, bool unscaledTime = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按资源地址加载预制体后，在世界坐标生成，旋转为单位四元数。
        /// </summary>
        /// <param name="assetLocation">YooAsset 资源定位地址</param>
        /// <param name="cancellationToken">只取消本次等待，不释放资源句柄；已取消则抛 OperationCanceledException</param>
        UniTask<FxHandle> SpawnAsync(string assetLocation, Vector3 worldPosition, float lifetimeSec, bool unscaledTime = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按资源地址加载预制体后，挂到父节点并按本地坐标生成。
        /// </summary>
        /// <param name="assetLocation">YooAsset 资源定位地址</param>
        /// <param name="parent">挂点；为空则返回空句柄</param>
        /// <param name="cancellationToken">只取消本次等待，不释放资源句柄；已取消则抛 OperationCanceledException</param>
        UniTask<FxHandle> SpawnAsync(string assetLocation, Transform parent, Vector3 localPosition, Quaternion localRotation, float lifetimeSec, bool unscaledTime = false, CancellationToken cancellationToken = default);

        /// <summary>
        /// 槽位仍对应该创建号，且实例还在。
        /// </summary>
        bool IsValid(FxHandle handle);

        /// <summary>
        /// 归还到对象池。
        /// </summary>
        void Stop(FxHandle handle);

        /// <summary>
        /// 设置世界坐标。
        /// </summary>
        void SetWorldPosition(FxHandle handle, Vector3 position);

        /// <summary>
        /// 设置相对当前父节点的本地坐标。
        /// </summary>
        void SetLocalPosition(FxHandle handle, Vector3 localPosition);

        /// <summary>
        /// 设置世界旋转。
        /// </summary>
        void SetWorldRotation(FxHandle handle, Quaternion rotation);

        /// <summary>
        /// 设置相对当前父节点的本地旋转。
        /// </summary>
        void SetLocalRotation(FxHandle handle, Quaternion localRotation);

        /// <summary>
        /// 设置根节点本地缩放。
        /// </summary>
        void SetLocalScale(FxHandle handle, Vector3 scale);

        /// <summary>
        /// 挂到父节点下。父节点被销毁时，这条特效会在帧末被收回。
        /// </summary>
        /// <param name="parent">挂点；为空则记错误并返回</param>
        /// <param name="worldPositionStays">为 true 时保持当前世界位姿</param>
        void Attach(FxHandle handle, Transform parent, bool worldPositionStays = true);

        /// <summary>
        /// 从父节点摘下，挂回特效池根节点并保持世界坐标。
        /// </summary>
        void Detach(FxHandle handle);

        /// <summary>
        /// 从现在起重设自动回收时间。小于等于 0 则改为一直播放到 Stop 或父节点失效。
        /// </summary>
        void ResetLifetime(FxHandle handle, float lifetimeSec);

        /// <summary>
        /// 同时存活的实例上限，包含生成中已预留的实例。小于等于 0 表示不限制。只拒绝此后的新实例。
        /// </summary>
        void SetGlobalCap(int maxLive);

        /// <summary>
        /// 按预制体名设置同时存活上限。小于等于 0 表示去掉该项。键与预制体资产名一致。
        /// </summary>
        void SetPrefabCap(string prefabName, int maxLive);

        /// <summary>
        /// 预热，转发到 GameObject 池。
        /// </summary>
        void Prewarm(GameObject prefab, int count);

        /// <summary>
        /// 异步预热已加载的预制体，转发到 GameObject 池。
        /// </summary>
        UniTask PrewarmAsync(GameObject prefab, int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// 按资源地址异步预热，转发到 GameObject 池。
        /// </summary>
        UniTask PrewarmAsync(string assetLocation, int count, CancellationToken cancellationToken = default);

        /// <summary>
        /// 归还调用时的全部在播特效，取消生成中的租约。回调中新生成的特效不在本次范围内。
        /// 生成中的实例由原生成调用退出时归还。GameObject 池里的闲置实例保留。
        /// </summary>
        void StopAll();

        /// <summary>
        /// 归还调用时该预制体资产的在播实例，并取消对应的生成中租约。回调中新生成的特效不在本次范围内。
        /// 参数是预制体，不是已生成的实例；生成中的实例由原生成调用退出时归还。
        /// </summary>
        void StopByPrefab(GameObject prefab);
    }
}
