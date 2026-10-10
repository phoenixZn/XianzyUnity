using UnityEngine;
using Xease.UI;

namespace Xease
{
    //////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// UI 面板服务：按预制体名打开/关闭 Panel，生命周期由 ServicesProvider 驱动。
    /// </summary>
    public interface IUIService : IService
    {
        /// <summary>
        /// UI 根上的相机；运行时根创建之后可用。
        /// </summary>
        Camera UICamera { get; }

        /// <summary>
        /// 异步打开面板；name 为 <see cref="UIBaseHandlerAttribute.PrefabName"/>（YooAsset location）。
        /// </summary>
        void Show(string name, object[] args = null);

        /// <summary>
        /// 关闭并销毁已打开的面板。
        /// </summary>
        void Hide(string name);

        /// <summary>
        /// 同步打开面板；仅用于必须立刻拿到实例的路径。
        /// </summary>
        UIPanelBase ShowSync(string name, object[] args = null);
    }
}
