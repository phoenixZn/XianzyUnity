using UnityEngine;

namespace Xease.UI
{
    /// <summary>
    /// 场景或运行时创建的 UI 根：绑定 Camera 与 Canvas。
    /// </summary>
    public class UIRoot
    {
        Camera _camera;
        public Camera camera => _camera;

        Canvas _canvas;
        public Canvas canvas => _canvas;

        public GameObject gameObject;

        public Transform transform => gameObject.transform;

        /// <summary>
        /// 从子节点解析 Camera / Canvas；缺哪个就 GetComponentInChildren。
        /// </summary>
        public void Init()
        {
            _camera = _camera ?? transform.GetComponentInChildren<Camera>();
            _canvas = _canvas ?? transform.GetComponentInChildren<Canvas>();
            if (_canvas == null)
                _canvas = gameObject.GetComponent<Canvas>();
        }
    }
}
