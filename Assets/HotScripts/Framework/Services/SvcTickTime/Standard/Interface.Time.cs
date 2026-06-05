using System;

namespace Xease
{

    //////////////////////////////////////////////////////////////////////////
    
    // 值类型事件系统接口
    public interface ITimeService : IService
    {
        public float fixedDeltaTime { get; }
        public float fixedUnscaledDeltaTime { get; }
        public float deltaTime { get; }
        public float unscaledDeltaTime { get; }
        public float timeScale { get; set; }
    }

}