#if !CONSOLE_CLIENT
using Unity.Profiling;

namespace Xease
{
    public partial class EnvDriver
    {
        ProfilerMarker _fixedUpdateMarker;
        ProfilerMarker _updateMarker;
        ProfilerMarker _lateUpdateMarker;

        partial void InitProfilerMarkers(string groupName)
        {
            _fixedUpdateMarker = new ProfilerMarker($"[{groupName}]:FixedUpdate");
            _updateMarker = new ProfilerMarker($"[{groupName}]:Update");
            _lateUpdateMarker = new ProfilerMarker($"[{groupName}]:LateUpdate");
        }

        partial void ProfileEnvFixedUpdateBegin() => _fixedUpdateMarker.Begin();
        partial void ProfileEnvFixedUpdateEnd() => _fixedUpdateMarker.End();
        partial void ProfileEnvUpdateBegin() => _updateMarker.Begin();
        partial void ProfileEnvUpdateEnd() => _updateMarker.End();
        partial void ProfileEnvLateUpdateBegin() => _lateUpdateMarker.Begin();
        partial void ProfileEnvLateUpdateEnd() => _lateUpdateMarker.End();
    }
}
#endif
