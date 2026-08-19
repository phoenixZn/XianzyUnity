using System;
using System.Collections.Generic;

namespace DesperateDevs.Caching
{
    /// <summary>
    /// 栈式对象池。供 Entitas Context 复用 GroupChanged 列表；逻辑来自早期 Entitas 内嵌实现。
    /// </summary>
    public class ObjectPool<T>
    {
        // 空池时创建新实例
        private readonly Func<T> _factoryMethod;
        // 归还前可选重置；为 null 则跳过
        private readonly Action<T> _resetMethod;
        // 已归还实例
        private readonly Stack<T> _objectPool;

        /// <summary>
        /// 用工厂方法创建池；resetMethod 在 Push 时调用。
        /// </summary>
        public ObjectPool(Func<T> factoryMethod, Action<T> resetMethod = null)
        {
            this._factoryMethod = factoryMethod;
            this._resetMethod = resetMethod;
            this._objectPool = new Stack<T>();
        }

        /// <summary>
        /// 取出一个实例；池空则走工厂方法。
        /// </summary>
        public T Get()
        {
            if (this._objectPool.Count != 0)
            {
                return this._objectPool.Pop();
            }
            return this._factoryMethod();
        }

        /// <summary>
        /// 归还实例；若构造时提供了 resetMethod 则先重置。
        /// </summary>
        public void Push(T obj)
        {
            if (this._resetMethod != null)
            {
                this._resetMethod(obj);
            }
            this._objectPool.Push(obj);
        }
    }
}
