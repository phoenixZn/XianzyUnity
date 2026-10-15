using System;
using Xease.CoreGame;

namespace Xease
{
    //////////////////////////////////////////////////////////////////////////
    public interface ICustomLogicService : IService
    {
        void AddConfigContainer(ILogicConfigContainer container);
        ILogicConfigContainer GetConfigContainer(string name);
        CustomLogic CreateLogic(CustomLogicGenInfo genInfo);
        T CreateLogic<T>(CustomLogicGenInfo genInfo) where T : CustomLogic;
        void DestroyLogic(CustomLogic logic);
        public T NewGenInfo<T>() where T : CustomLogicGenInfo, new();
        /// <summary>回收尚未交给 Logic 的 GenInfo；CreateLogic 成功后改由 DestroyLogic 回收。</summary>
        void DestroyGenInfo(CustomLogicGenInfo genInfo);
        public VarEnv NewVarEnv();
    }

}