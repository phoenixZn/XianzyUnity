using UnityEngine;

namespace Xease
{
    public partial class GameEntry
    {
        partial void TryCreateGameEnv()
        {
            if (GEnv.Inst != null)
            {
                Debug.LogError("TryCreateGameEnv GEnv.Inst != null");
                return;
            }
            Debug.Log("初始化游戏环境 GEnv");
            var param = new GEnvParam()
            {
                LogInfo = Debug.Log,
                LogWarning = Debug.LogWarning,
                LogError = Debug.LogError,
                EnvBaseSeed = Time.frameCount,
            };
            GEnv.InitGameEnvInstance(new UnityGameEnv(param));
        }
    }
}
