#if !CONSOLE_CLIENT
using UnityEngine;
#endif

namespace Xease
{
    public delegate void GEnvLogAction(string info);
    //注入的游戏环境初始参数
    public class GEnvParam
    {
        private static void EmptyLog(string info) { }
        public GEnvLogAction LogError = EmptyLog;
        public GEnvLogAction LogWarning = EmptyLog;
        public GEnvLogAction LogInfo = EmptyLog;
        public int EnvBaseSeed = 1;
#if !CONSOLE_CLIENT
        // Unity 宿主入口（通常是 GameEntry），供需要 MonoBehaviour 的服务在初始化时注入，如协程宿主
        public MonoBehaviour UnityHost;
#endif
    }
}
