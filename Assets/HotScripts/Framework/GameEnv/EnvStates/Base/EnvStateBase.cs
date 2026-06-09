namespace Xease
{
    public partial class EnvStateID
    {
    }
    
    public class EnvStateBase : IEnvUpdate
    {
        protected EnvStateManager _stateMgr;
        protected string _nextStateID;
        public string StateID { get; private set; }

        public EnvTransferWorks TransferWorks { get; protected set; }

        public virtual void Init(EnvStateManager stateMgr, string stateID)
        {
            _stateMgr = stateMgr;
            StateID = stateID;
            _nextStateID = null;
        }


        public virtual void Enter(EnvStateBase fromState)
        {
            G.Log($"EnvState [{StateID}] : Enter");
        }
        

        public virtual void Leave(EnvStateBase toState)
        {
            G.Log($"EnvState [{StateID}] : Leave");
            CreateTransferWork();
            _nextStateID = null;
        }

        /// <summary>
        /// 处理如果从当前状态直接销毁了要做的事儿
        /// </summary>
        public virtual void OnDestroy()
        {
            
        }
        
        public virtual void EnvUpdate(float dt, float dt_unscaled)
        {
        }
        
        public virtual string CheckTransitions()
        {
            return _nextStateID;
        }
        
        public virtual bool CantInterrupt()
        {
            return false;
        }

        public virtual void CreateTransferWork()
        {
            TransferWorks = null;
        }

        public virtual EnvTransferWorks FilterWorks(EnvTransferWorks work)
        {
            if (work == null)
            {
                return null;
            }
            return work;
        }

        /// <summary>
        /// 运行上一个阶段
        /// </summary>
        protected virtual void RunLastStateWorks(EnvTransferWorks work)
        {
            if (work == null)
            {
                return;
            }
            if (work.Works != null)
            {
                for (int i = 0; i < work.Works.Count; i++)
                {
                    work.Works[i].Run(this);
                }
            }
        }
        
        protected object TakeAwayParam()
        {
            return _stateMgr.TakeAwayParam();
        }
        public void SetParam(object param)
        {
            _stateMgr.SetParam(param);
        }
    }
}
