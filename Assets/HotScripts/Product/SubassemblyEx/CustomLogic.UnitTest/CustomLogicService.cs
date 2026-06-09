namespace Xease.CoreGame
{
    public interface ICustomLogicService
    {
        void AddConfigContainer(ILogicConfigContainer container);
        ILogicConfigContainer GetConfigContainer(string name);
        CustomLogic CreateLogic(ICustomLogicGenInfo genInfo);
        T CreateLogic<T>(ICustomLogicGenInfo genInfo) where T : CustomLogic;
        void DestroyLogic(CustomLogic logic);
        public T NewGenInfo<T>() where T : ICustomLogicGenInfo, new();
        public VarEnv NewVarEnv();
    }
    
    
    
    public class CustomLogicService : ICustomLogicService
    {
        private CustomLogicFactory _factory;

        public CustomLogicService()
        {
            _factory = new CustomLogicFactory();
        }

        public void Init()
        {
        }

        public void Dispose()
        {
            _factory.Dispose();
        }

        public void AddConfigContainer(ILogicConfigContainer container)
        {
            _factory.AddConfigContainer(container);
        }

        public ILogicConfigContainer GetConfigContainer(string name)
        {
            return _factory.TryGetConfigContainer(name, out var container) ? container : null;
        }

        public CustomLogic CreateLogic(ICustomLogicGenInfo genInfo)
        {
            if (genInfo == null)
            {
                CLogger.LogError("CreateLogic genInfo == null");
                return null;
            }

            return _factory.CreateLogic(genInfo);
        }

        public T CreateLogic<T>(ICustomLogicGenInfo genInfo) where T : CustomLogic
        {
            if (genInfo == null)
            {
                CLogger.LogError($"CreateLogic<T> genInfo == null, T = {typeof(T)}");
                return null;
            }

            var logic = _factory.CreateLogic(genInfo);
            if (logic is T theLogic)
            {
                return theLogic;
            }
            CLogger.LogError($"CreateLogic logic = {genInfo.LogicConfigID} logic({logic.GetType()}) is not {typeof(T)}");
            return null;
        }

        public void DestroyLogic(CustomLogic logic)
        {
            if (logic == null)
                return;
            _factory.DestroyCustomNode(logic);
        }


        public T NewGenInfo<T>() where T : ICustomLogicGenInfo, new()
        {
            return _factory.CreatePart<T>();
        }

        public VarEnv NewVarEnv()
        {
            return _factory.CreatePart<VarEnv>();
        }
    }
}