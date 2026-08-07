using System;
using MackySoft.XPool;

namespace Xease
{
    /// <summary>
    /// 纯 C# 对象池服务：按类型集中管理 FactoryPool，提供租用/归还/预热/清空。
    /// </summary>
    public interface IPoolService : IService
    {
        // 未显式 Register 时懒创建池所用的默认容量
        const int DefaultCapacity = 16;

        /// <summary>
        /// 注册自定义池；需在该类型首次 Rent/GetPool 前调用，重复注册告警并忽略。
        /// </summary>
        void Register<T>(
            Func<T> factory,
            int capacity = DefaultCapacity,
            Action<T> onRent = null,
            Action<T> onReturn = null,
            Action<T> onRelease = null) where T : class, new();

        /// <summary>
        /// 租用实例；池空时按已注册工厂（默认 new）创建。
        /// </summary>
        T Rent<T>() where T : class, new();

        /// <summary>
        /// 归还实例；null 告警并忽略。
        /// </summary>
        void Return<T>(T instance) where T : class, new();

        /// <summary>
        /// 预热：先借后还 count 个实例，消除运行期创建尖刺。
        /// </summary>
        void Prewarm<T>(int count) where T : class, new();

        /// <summary>
        /// 清空指定类型池内全部实例。
        /// </summary>
        void Clear<T>() where T : class, new();

        /// <summary>
        /// 暴露底层池，便于使用 RentTemporary 等 MackySoft 扩展。
        /// </summary>
        IPool<T> GetPool<T>() where T : class, new();
    }
}
