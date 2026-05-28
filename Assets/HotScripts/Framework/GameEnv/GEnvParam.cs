namespace HotUpdate
{
    public delegate void GEnvLogAction(string info);
    //注入的游戏环境初始参数
    public partial class GEnvParam
    {
        private static void EmptyLog(string info) { }
        public GEnvLogAction LogError = EmptyLog;
        public GEnvLogAction LogInfo = EmptyLog;
    }
}
