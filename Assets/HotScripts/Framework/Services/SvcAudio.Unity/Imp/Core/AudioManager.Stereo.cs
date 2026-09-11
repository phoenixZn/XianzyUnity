namespace Xease.Audio
{
    public partial class AudioManager
    {
        //////////////////////////////////////////////////////////////////////////
        /// IAudioService:
        /// <summary>
        /// 立体声定位策略。
        /// </summary>
        public IAudioStereo Stereo { get; private set; } = new AudioStereoBase();

        /// <summary>
        /// 替换立体声定位实现。
        /// </summary>
        public void SetStereo(IAudioStereo stereo)
        {
            Stereo = stereo;
        }
    }
}