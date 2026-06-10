#if CONSOLE_CLIENT
namespace Xease
{
    public partial class EnvDriver
    {
        partial void InitProfilerMarkers(string groupName) { }
        partial void ProfileEnvFixedUpdateBegin() { }
        partial void ProfileEnvFixedUpdateEnd() { }
        partial void ProfileEnvUpdateBegin() { }
        partial void ProfileEnvUpdateEnd() { }
        partial void ProfileEnvLateUpdateBegin() { }
        partial void ProfileEnvLateUpdateEnd() { }
    }
}
#endif
