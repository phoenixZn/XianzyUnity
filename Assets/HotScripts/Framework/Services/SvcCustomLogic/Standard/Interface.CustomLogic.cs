using System;
using Xease.CoreGame;

namespace Xease
{
    //////////////////////////////////////////////////////////////////////////
    public interface ICustomLogicService : IService
    {
        void AddConfigContainer(ILogicConfigContainer container);
        ILogicConfigContainer GetConfigContainer(string name);
        CustomLogic CreateLogic(ICustomLogicGenInfo genInfo);
        T CreateLogic<T>(ICustomLogicGenInfo genInfo) where T : CustomLogic;
        void DestroyLogic(CustomLogic logic);
        public T NewGenInfo<T>() where T : ICustomLogicGenInfo, new();
        public VarEnv NewVarEnv();
    }

}