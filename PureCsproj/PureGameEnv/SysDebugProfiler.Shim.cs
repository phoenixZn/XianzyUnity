using Entitas;

namespace Xease.CoreGame.Debug
{
    /// <summary>
    /// 替代 Product 侧 Profiler.Unity/SysDebugProfiler（已从本工程排除）；
    /// 保留构造与系统接口，使 WorldsConfig.Main 仍能编译。
    /// </summary>
    public partial class SysDebugProfiler : ECWorldSystem, IInitializeSystem, IExecuteSystem, ITearDownSystem, IUpdateSystem
    {
        /// <summary>
        /// 与 Assets 侧实现相同的 ECWorld 绑定入口。
        /// </summary>
        public SysDebugProfiler(ECWorlds worlds) : base(worlds)
        {
        }

        /// <summary>
        /// 纯 C# 工程占位；Profiler 采样不在此执行。
        /// </summary>
        public void Initialize()
        {
        }

        /// <summary>
        /// 纯 C# 工程占位；Profiler 采样不在此执行。
        /// </summary>
        public void Execute()
        {
        }

        /// <summary>
        /// 纯 C# 工程占位；Profiler 采样不在此执行。
        /// </summary>
        public void Update(float dt, float dt_unscaled)
        {
        }

        /// <summary>
        /// 纯 C# 工程占位。
        /// </summary>
        public void TearDown()
        {
        }
    }
}
