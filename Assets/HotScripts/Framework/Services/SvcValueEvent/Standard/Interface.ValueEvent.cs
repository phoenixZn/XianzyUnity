using System;

namespace Xease
{

    //////////////////////////////////////////////////////////////////////////
    
    
    //值类型事件: (标识性质接口)
    public interface IValueEvent
    {
    }

    // 值类型事件系统接口
    public interface IValueEventService : IService
    {
        void Dispatch<T>(T evt) where T : struct, IValueEvent;
        void AddHandler<T>(Action<T> handler) where T : struct, IValueEvent;
        void RemoveHandler<T>(Action<T> handler) where T : struct, IValueEvent;
    }
}