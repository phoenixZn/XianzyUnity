using System;
using System.Collections.Generic;
//////////////////////////////////////////////////////////////////////////
/*
    这里定义 Framework 程序集中依赖的 Service 的接口
    项目同一概念的 Service 想要换一个实现，只要满足这里定义的 Service 接口就能够在框架代码下运行
    项目不要修改 Service 接口
*/
////////////////////////////////////////////////////////////////////////// 


namespace Xease
{
    /// <summary>
    /// 底层服务抽象，和具体业务无关。是可以独立存在的服务供应者
    /// </summary>
    public interface IService
    {
        void Shutdown();
    }
    
    //////////////////////////////////////////////////////////////////////////
    // Service: Timer
    public interface ITimerService : IService
    {
        public int AddTimer(float interval, int times, Action<float, int> cb, bool ignoreTimeScale = false);
        public bool RemoveTimer(int id);
        public bool ResetTimer(int id, float interval = -1f);
    }

    //////////////////////////////////////////////////////////////////////////
    // Service: 类型缓存
    public interface ITypeCacheService : IService
    {
        public HashSet<Type> GetTypesByAttribute(Type systemAttributeType);
        public Dictionary<string, Type> GetTypes();
        public Type GetType(string typeName);
    }
    
}