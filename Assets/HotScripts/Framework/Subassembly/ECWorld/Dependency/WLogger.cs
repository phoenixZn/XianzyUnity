using System;

namespace Xease.CoreGame
{
    public static partial class WLogger
    {
        public static bool IsDev { get; set; } = true;
        
#if CONSOLE_CLIENT
        public static void Log(string info)
        {
            Console.WriteLine($"[Info] {info}");
        }
        public static void LogError(string info)
        {
            Console.WriteLine($"[Error] {info}");
        }
        public static void LogWarning(string info)
        {
            Console.WriteLine($"[Warning] {info}");
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