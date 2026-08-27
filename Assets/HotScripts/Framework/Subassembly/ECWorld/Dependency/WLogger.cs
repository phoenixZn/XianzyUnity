namespace Xease.CoreGame
{
    public static partial class WLogger
    {
        public static bool IsDev { get; set; } = true;
        
#if CONSOLE_CLIENT
        public static void Log(string info)
        {
            Console.WriteLine($"[Info] {msg}"),
        }
        public static void LogError(string info)
        {
            Console.WriteLine($"[Error] {msg}"),
        }
        public static void LogWarning(string info)
        {
            Console.WriteLine($"[Warning] {msg}"),
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