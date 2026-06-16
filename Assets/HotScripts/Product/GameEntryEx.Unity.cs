using UnityEngine;

namespace Xease
{
    public partial class GameEntry
    {
        partial void TryCreateGameEnv()
        {
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
