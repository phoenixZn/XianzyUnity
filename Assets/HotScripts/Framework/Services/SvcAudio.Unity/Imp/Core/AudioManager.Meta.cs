namespace Xease.Audio
{
    public partial class AudioManager
    {
        // 从 RawFile 加载的元配置；Shutdown 后清空
        private AudioMetaConfig _audioMeta;

        //////////////////////////////////////////////////////////////////////////
        /// IAudioService:
        /// <summary>
        /// 当前音频元配置；未初始化时为 null。
        /// </summary>
        public AudioMetaConfig Meta => _audioMeta;

        //////////////////////////////////////////////////////////////////////////
        /// This：
        private void InitMeta()
        {
            _audioMeta = AudioRawFile<AudioMetaConfig>.Load(AudioMetaConfig.Path);
        }

        private void DisposeMeta()
        {
            _audioMeta = null;
        }
    }
}