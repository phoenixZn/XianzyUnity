namespace LitMotion
{
    /// <summary>
    /// PlayerLoop 插入点占位（CLI 不注入 PlayerLoop，仅保留类型供 MotionScheduler 字段编译）。
    /// </summary>
    public static class LitMotionLoopRunners
    {
        /// <summary>Initialization 插入点占位。</summary>
        public struct LitMotionInitialization { }
        /// <summary>EarlyUpdate 插入点占位。</summary>
        public struct LitMotionEarlyUpdate { }
        /// <summary>FixedUpdate 插入点占位。</summary>
        public struct LitMotionFixedUpdate { }
        /// <summary>PreUpdate 插入点占位。</summary>
        public struct LitMotionPreUpdate { }
        /// <summary>Update 插入点占位。</summary>
        public struct LitMotionUpdate { }
        /// <summary>PreLateUpdate 插入点占位。</summary>
        public struct LitMotionPreLateUpdate { }
        /// <summary>PostLateUpdate 插入点占位。</summary>
        public struct LitMotionPostLateUpdate { }
        /// <summary>TimeUpdate 插入点占位。</summary>
        public struct LitMotionTimeUpdate { }
    }

    // CLI 调度只走 Manual；此枚举供 MotionDispatcher / PlayerLoopMotionScheduler 编译
    internal enum PlayerLoopTiming
    {
        Initialization = 0,
        EarlyUpdate = 1,
        FixedUpdate = 2,
        PreUpdate = 3,
        Update = 4,
        PreLateUpdate = 5,
        PostLateUpdate = 6,
        TimeUpdate = 7,
    }
}
