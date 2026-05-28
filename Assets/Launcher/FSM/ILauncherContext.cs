using System.Collections;

namespace Launcher
{
    public delegate void LauncherLogAction(string info);
    
    //////////////////////////////////////////////////////////////////////////
    // Launcher状态共享上下文
    public interface ILauncherContext
    {
        void SetBlackboardValue(string key, object value);
        object GetBlackboardValue(string key, object defaultValue = null);

        LauncherLogAction LogError { get; }
        LauncherLogAction LogInfo { get;  }

        //////////////////////////////////////////////////////////////////////////
        //UnityEngine 依赖
        UnityEngine.Coroutine StartCoroutine(IEnumerator routine);
    }
}
