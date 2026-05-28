namespace Launcher
{
    public class LauncherState
    {
        protected ILauncherContext _contextRef;
        protected string _stateID;

        public string StateID
        {
            get
            {
                return _stateID;
            }
        }

        public virtual void Init(ILauncherContext context, string stateID)
        {
            _contextRef = context;
            _stateID = stateID;
        }

        public virtual void Enter()
        {
            _contextRef.LogInfo($"xCore: LState LauncherState Enter:{_stateID}");
        }

        public virtual void Leave()
        {
        }

        public virtual void Update(float dt)
        {
        }

        public virtual string CheckTransitions()
        {
            return _stateID; // 默认保持当前状态
        }
    }
}
