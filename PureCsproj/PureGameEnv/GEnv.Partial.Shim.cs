namespace HotUpdate
{
    /// <summary>
    /// 替代 Product/GEnv.Ex.cs（其中依赖 AssetService 等 Unity 侧实现，已从本工程编译中排除）；
    /// 与排除的 GEnvEx.Unity 对等的纯控制台/命令行空实现。
    /// </summary>
    public class ConsoleGameEnv : GEnv
    {
        public ConsoleGameEnv(GEnvParam param) : base(param)
        {
        }

        protected override void Inner_CreateServices()
        {
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
