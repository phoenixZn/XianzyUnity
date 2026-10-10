using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 相机服务：持有缺省 URP Base 相机，把 UI 相机作为 Overlay 叠到当前 Base 的 cameraStack。
    /// </summary>
    public interface ICameraService : IService
    {
        /// <summary>
        /// 当前 Base 相机；可能是场景相机或自持的缺省相机。
        /// </summary>
        Camera Current { get; }

        /// <summary>
        /// 服务自建并持有的缺省 Base 相机。场景相机在用时该对象处于禁用。
        /// </summary>
        Camera DefaultCamera { get; }

        /// <summary>
        /// 初始化时接入的 UI 相机，作为 Overlay 使用。
        /// </summary>
        Camera UICamera { get; }

        /// <summary>
        /// 切换当前 Base 相机，并把 UI 相机放进其 cameraStack。
        /// 传入自持缺省相机时启用它，否则禁用缺省相机。
        /// </summary>
        /// <param name="camera">要作为当前 Base 的相机。</param>
        void SetCurrentCamera(Camera camera);
    }
}
