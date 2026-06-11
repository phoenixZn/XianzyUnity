namespace Xease.CoreGame
{
    public static partial class WLogger
    {
#if CONSOLE_CLIENT

        public static void Log(string info)
        {
        }
        public static void LogError(string info)
        {
        }
        public static void LogWarning(string info)
        {
        }
#else
        public static void Log(string info)
        {
            UnityEngine.Debug.Log(info);
        }
        public static void LogError(string info)
        {
            UnityEngine.Debug.LogError(info);
        }
        public static void LogWarning(string info)
        {
            UnityEngine.Debug.LogWarning(info);
        }
#endif
    }
}