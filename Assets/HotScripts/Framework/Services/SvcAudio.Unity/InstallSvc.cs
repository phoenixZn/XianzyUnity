using Xease.Audio;

namespace Xease
{
    public static partial class G
    {
        public static IAudioService Audio => GEnv.Inst.Services.AudioSvc;
    }

    public partial class ServicesProvider
    {
        //////////////////////////////////////////////////////////////////////////
        /// 音频服务：
        protected IAudioService _audioSvc;
        public IAudioService AudioSvc
        {
            get { return _audioSvc; }
        }

        public void AddService_Audio()
        {
            G.Log("AddService_Audio");
            var svc = new AudioManager();
            svc.Init();
            AddService(svc, out _audioSvc);
        }
    }
}
