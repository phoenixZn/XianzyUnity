using UnityEngine;

namespace Xease
{
    public partial class GEnvParam
    {
        // Unity 宿主入口（通常是 GameEntry），供需要 MonoBehaviour 的服务在初始化时注入，如协程宿主
        public MonoBehaviour UnityHost;
    }
}
