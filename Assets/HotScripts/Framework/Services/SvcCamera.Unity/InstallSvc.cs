using UnityEngine;

namespace Xease
{
    public static partial class G
    {
        public static ICameraService Camera => GEnv.Inst.Services.CameraSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// 相机服务：
        protected ICameraService _cameraSvc;
        public ICameraService CameraSvc => _cameraSvc;

        /// <summary>
        /// 注册相机服务；uiCamera 为 UIManager 已创建的 UI 相机。未传镜头参数时用 <see cref="CameraCreateParam.Default"/>。
        /// </summary>
        public void AddService_Camera(Camera uiCamera)
        {
            AddService_Camera(uiCamera, CameraCreateParam.Default);
        }

        /// <summary>
        /// 注册相机服务，并用 param 创建缺省 Base 相机。
        /// </summary>
        public void AddService_Camera(Camera uiCamera, CameraCreateParam param)
        {
            G.Log("AddService_Camera");
            var svc = new CameraService();
            svc.Init(uiCamera, param);
            AddService(svc, out _cameraSvc);
        }
    }
}
