using UnityEngine;

namespace Xease
{
    /// <summary>
    /// 缺省 Base 相机的创建参数。未传入时使用 <see cref="Default"/>。
    /// </summary>
    public struct CameraCreateParam
    {
        const string UiLayerName = "UI"; // 与 UI 服务的层同名，Default 的 CullingMask 排除该层

        public CameraClearFlags ClearFlags; // 清屏方式
        public Color BackgroundColor; // ClearFlags 为 SolidColor 时的背景色
        public float FieldOfView; // 垂直视场角，度
        public float NearClipPlane; // 近裁剪面，世界单位
        public float FarClipPlane; // 远裁剪面，世界单位
        public bool Orthographic; // true 时使用正交投影
        public float OrthographicSize; // 正交半高，世界单位
        public float Depth; // 同类型相机的绘制先后，值越小越先画
        public int CullingMask; // 可见层
        public Vector3 LocalPosition; // 缺省相机局部位置
        public Quaternion LocalRotation; // 缺省相机局部旋转

        /// <summary>
        /// 未传入参数时的缺省镜头。CullingMask 排除 UI 层。
        /// </summary>
        public static CameraCreateParam Default
        {
            get
            {
                int mask = ~0;
                int uiLayer = LayerMask.NameToLayer(UiLayerName);
                if (uiLayer < 0)
                    G.LogError("[CameraService] layer UI is missing");
                else
                    mask &= ~(1 << uiLayer);

                return new CameraCreateParam
                {
                    ClearFlags = CameraClearFlags.SolidColor,
                    BackgroundColor = Color.black,
                    FieldOfView = 60f,
                    NearClipPlane = 0.3f,
                    FarClipPlane = 1000f,
                    Orthographic = false,
                    OrthographicSize = 5f,
                    Depth = -1f,
                    CullingMask = mask,
                    LocalPosition = Vector3.zero,
                    LocalRotation = Quaternion.identity,
                };
            }
        }
    }
}
