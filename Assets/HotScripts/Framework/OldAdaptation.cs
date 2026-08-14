//旧代码的适配集中在这里，便于快速编译通过，并阶段性修改

using Xease;

public static class KLogger
{
    public static void Log(string log)
    {
        G.Log(log);
    }
    
    public static void LogWarning(string log)
    {
        G.LogWarning(log);
    }
    
    public static void LogError(string log)
    {
        G.LogError(log);
    }
}