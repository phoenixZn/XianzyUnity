namespace Xease.Audio
{
    public partial class AudioManager
    {
        public IAudioStereo Stereo { get; private set; } = new AudioStereoBase();
        
        public void SetStereo(IAudioStereo stereo)
        {
            Stereo = stereo;
        }
    }
}