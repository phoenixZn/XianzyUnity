using System;

namespace Xease.UI
{
    /// <summary>
    /// 开合缩放动画；无 scaleRoot 时实现应直接返回。
    /// </summary>
    public interface IScaleable
    {
        /// <summary>
        /// 播放打开缩放。
        /// </summary>
        void DoScale();

        /// <summary>
        /// 播放关闭缩放，完成后回调 GameObject 名。
        /// </summary>
        void DoScaleReverse(Action<string> callBack);
    }
}
