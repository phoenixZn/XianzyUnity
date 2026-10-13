using UnityEngine;
using UnityEngine.UI;

namespace Xease.UI
{
    /// <summary>
    /// 按速度滚动 RawImage 的 uvRect。
    /// </summary>
    public class ImgUVRoll : MonoBehaviour
    {
        public float speed_x = 0.1f; // uv 每秒水平偏移
        public float speed_y = 0.1f; // uv 每秒垂直偏移

        RawImage _image;

        /// <summary>
        /// 缓存同节点上的 RawImage。
        /// </summary>
        public void Start()
        {
            _image = GetComponent<RawImage>();
        }

        /// <summary>
        /// 按速度改 uvRect 原点。
        /// </summary>
        public void Update()
        {
            if (_image == null)
                return;

            var rect = _image.uvRect;
            rect.x -= speed_x * Time.deltaTime;
            rect.y -= speed_y * Time.deltaTime;
            _image.uvRect = rect;
        }
    }
}
