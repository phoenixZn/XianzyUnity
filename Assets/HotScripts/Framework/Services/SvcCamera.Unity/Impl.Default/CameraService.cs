using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Xease
{
    /// <summary>
    /// 相机服务实现：自建缺省 URP Base 相机；场景已有 Base 相机时改用场景相机。
    /// 之后加载的场景出现新的 Base 相机时同样改绑，并把 UI 相机叠进 cameraStack。
    /// </summary>
    public class CameraService : ICameraService
    {
        const string DefaultCameraName = "[DefaultBaseCamera]";

        GameObject _defaultRoot; // 自持缺省 Base 相机根；切到场景相机时禁用，Shutdown 时销毁
        Camera _defaultCamera; // _defaultRoot 上的相机
        Camera _uiCamera; // 初始化接入的 UI 相机，作为 Overlay
        Camera _current; // 当前 Base 相机

        //////////////////////////////////////////////////////////////////////////
        /// IService:
        /// <summary>
        /// 卸掉场景回调，从当前栈移除 UI 相机，并销毁自持的缺省相机。
        /// </summary>
        public void Shutdown()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            DetachUiOverlay(_current);
            if (_defaultRoot != null)
                Object.Destroy(_defaultRoot);
            _defaultRoot = null;
            _defaultCamera = null;
            _current = null;
            _uiCamera = null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// ICameraService:
        /// <summary>
        /// 当前 Base 相机；可能是场景相机或自持的缺省相机。
        /// </summary>
        public Camera Current => _current;

        /// <summary>
        /// 服务自建并持有的缺省 Base 相机。场景相机在用时该对象处于禁用。
        /// </summary>
        public Camera DefaultCamera => _defaultCamera;

        /// <summary>
        /// 初始化时接入的 UI 相机，作为 Overlay 使用。
        /// </summary>
        public Camera UICamera => _uiCamera;

        /// <summary>
        /// 切换当前 Base 相机，并把 UI 相机放进其 cameraStack。
        /// 传入自持缺省相机时启用它，否则禁用缺省相机。
        /// </summary>
        public void SetCurrentCamera(Camera camera)
        {
            if (camera == null || camera == _uiCamera || camera == _current)
                return;

            DetachUiOverlay(_current);
            if (_defaultRoot != null)
                _defaultRoot.SetActive(camera == _defaultCamera);

            _current = camera;
            var data = camera.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            AttachUiOverlay(camera);
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        /// <summary>
        /// 创建缺省 Base 相机，接入 UI 相机，并按当前已加载场景决定当前相机。
        /// </summary>
        /// <param name="uiCamera">UIManager 保证存在的 UI 相机。</param>
        /// <param name="param">缺省 Base 相机参数。</param>
        public void Init(Camera uiCamera, CameraCreateParam param)
        {
            _uiCamera = uiCamera;
            if (_uiCamera == null)
                G.LogError("[CameraService] UI camera is null");

            CreateDefault(param);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            var sceneCam = FindAnyLoadedSceneBaseCamera();
            if (sceneCam != null)
            {
                SetCurrentCamera(sceneCam);
                return;
            }

            SetCurrentCamera(_defaultCamera);
        }

        // 跨场景持有；存在场景 Base 相机时由 SetCurrentCamera 禁用
        void CreateDefault(CameraCreateParam param)
        {
            var go = new GameObject(DefaultCameraName);
            Object.DontDestroyOnLoad(go);
            go.transform.localPosition = param.LocalPosition;
            go.transform.localRotation = param.LocalRotation;

            var cam = go.AddComponent<Camera>();
            cam.clearFlags = param.ClearFlags;
            cam.backgroundColor = param.BackgroundColor;
            cam.fieldOfView = param.FieldOfView;
            cam.nearClipPlane = param.NearClipPlane;
            cam.farClipPlane = param.FarClipPlane;
            cam.orthographic = param.Orthographic;
            cam.orthographicSize = param.OrthographicSize;
            cam.depth = param.Depth;
            cam.cullingMask = param.CullingMask;
            go.AddComponent<AudioListener>();

            var data = cam.GetUniversalAdditionalCameraData();
            data.renderType = CameraRenderType.Base;
            _defaultRoot = go;
            _defaultCamera = cam;
        }

        // 新场景出现 Base 相机时改绑；没有则保持当前
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var cam = FindSceneBaseCamera(scene);
            if (cam == null)
                return;
            SetCurrentCamera(cam);
        }

        // 已加载场景里的 URP Base 相机；多台时优先 MainCamera
        Camera FindAnyLoadedSceneBaseCamera()
        {
            Camera tagged = null;
            Camera first = null;
            for (int i = 0; i < SceneManager.sceneCount; i++)
                Consider(SceneManager.GetSceneAt(i), ref tagged, ref first);
            if (tagged != null)
                return tagged;
            return first;
        }

        Camera FindSceneBaseCamera(Scene scene)
        {
            Camera tagged = null;
            Camera first = null;
            Consider(scene, ref tagged, ref first);
            if (tagged != null)
                return tagged;
            return first;
        }

        void Consider(Scene scene, ref Camera tagged, ref Camera first)
        {
            if (tagged != null)
                return;
            if (!scene.IsValid() || !scene.isLoaded)
                return;

            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var cams = roots[i].GetComponentsInChildren<Camera>(false);
                for (int j = 0; j < cams.Length; j++)
                {
                    var cam = cams[j];
                    if (!IsSceneBaseCamera(cam))
                        continue;
                    if (cam.CompareTag("MainCamera"))
                    {
                        tagged = cam;
                        return;
                    }

                    if (first == null)
                        first = cam;
                }
            }
        }

        // URP Base，且不是 UI 相机和自持缺省相机
        bool IsSceneBaseCamera(Camera cam)
        {
            if (cam == null || cam == _uiCamera || cam == _defaultCamera)
                return false;
            if (!cam.isActiveAndEnabled)
                return false;
            var data = cam.GetComponent<UniversalAdditionalCameraData>();
            return data != null && data.renderType == CameraRenderType.Base;
        }

        void DetachUiOverlay(Camera baseCam)
        {
            if (baseCam == null || _uiCamera == null)
                return;
            var data = baseCam.GetComponent<UniversalAdditionalCameraData>();
            if (data == null || data.renderType != CameraRenderType.Base || data.scriptableRenderer == null)
                return;
            var stack = data.cameraStack;
            if (stack == null)
                return;
            stack.Remove(_uiCamera);
        }

        // UI 相机改为 Overlay 后进入当前 Base 的 cameraStack
        void AttachUiOverlay(Camera baseCam)
        {
            if (baseCam == null || _uiCamera == null)
                return;
            var uiData = _uiCamera.GetUniversalAdditionalCameraData();
            uiData.renderType = CameraRenderType.Overlay;

            var baseData = baseCam.GetUniversalAdditionalCameraData();
            if (baseData.scriptableRenderer == null)
            {
                G.LogError("[CameraService] URP renderer is missing");
                return;
            }

            var stack = baseData.cameraStack;
            if (stack == null || stack.Contains(_uiCamera))
                return;
            stack.Add(_uiCamera);
        }
    }
}
