using System;
using System.IO;
using System.Text;
#if CONSOLE_CLIENT
using System.Diagnostics;
#else
using UnityEngine;
#endif



// public interface ILogWriter
// {
//     void WriteLog(string line);
// }


public class CLogger
{
    public static string sm_product_prefix = "GameCilent";
    public static string sm_log_prefix = ": " + sm_product_prefix;
    
    public enum ELogLevel
    {
        DEBUG = 800,
        INFO = 700,
        WARNING = 500,
        ERROR = 400,
        CRITICAL = 300,
    }

    private static ELogLevel m_log_level = ELogLevel.DEBUG;
    private static bool mEnabled = true;


    public static bool Enabled
    {
        get { return mEnabled; }
        set { mEnabled = value; }
    }

    static bool CheckLogLevel(ELogLevel level)
    {
        if (mEnabled && m_log_level >= level)
        {
            return true;
        }
        return false;
    }
    
    private static void WriteLogLine(string levelname, ELogLevel level, string info)
    {
        if (!CheckLogLevel(level))
            return;

        //-----------------------
#if CONSOLE_CLIENT
        string line = string.Format("{1} {2} {3} - {0}", info, DateTime.Now.ToString(), sm_log_prefix, levelname);
        Console.WriteLine( line );
#else
        switch (level)
        {
            case ELogLevel.DEBUG:
                Debug.Log(info);
                break;
            case ELogLevel.INFO:
                Debug.Log(info);
                break;
            case ELogLevel.WARNING:
                Debug.LogWarning(info);
                break;
            case ELogLevel.ERROR:
                Debug.LogError(info);
                break;
            case ELogLevel.CRITICAL:
                Debug.LogError(info);
                break;
            default:
                Debug.Log(info);
                break;
        }
#endif
    }

    
    public static void LogDebug(string info)
    {
        WriteLogLine("Debug", ELogLevel.DEBUG, info);
    }
    
    public static void LogInfo(string info)
    {
        WriteLogLine("Info", ELogLevel.INFO, info);
    }
    
    public static void LogWarning(string info)
    {
        WriteLogLine("Warning", ELogLevel.WARNING, info);
    }
    
    public static void LogError(string info)
    {
        WriteLogLine("Error", ELogLevel.ERROR, info);
    }
    
    public static void LogErrorDebugWrapper(string info)
    {
        string line = string.Format("{0}{1}{2}", "<color=#22BB00FF>DEBUG_ONLY: ", info, "</color>");
        WriteLogLine("Error", ELogLevel.ERROR, line);
    }
}