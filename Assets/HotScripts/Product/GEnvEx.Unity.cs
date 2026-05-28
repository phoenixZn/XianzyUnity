namespace HotUpdate
{
    // 实现约束下的快捷访问, 项目特化的不往里扔 （满足快捷访问的需求，留个方便重构的退路）
    public static partial class G
    {
    }

    //////////////////////////////////////////////////////////////////////////
    /// 游戏全局环境（Unity 宿主实现）
    //////////////////////////////////////////////////////////////////////////
    public class UnityGameEnv : GEnv
    {
        public UnityGameEnv(GEnvParam param) : base(param)
        {
        }

        protected override void Inner_CreateServices()
        {
            AddService_ValueEvent();
            AddService_Coroutine();
            AddService_Asset();
        }

        protected override void Inner_CreateModules()
        {
        }

        protected override void Inner_CreateManagers()
        {
        }

        protected override void Inner_UpdateEnv(float deltaTime, float unscaledDeltaTime)
        {
        }

        protected override void Inner_LateUpdateEnv()
        {
        }

        protected override void Inner_DrawGizmosEnv()
        {
        }

        protected override void Inner_PreClear()
        {
        }
    }
}
