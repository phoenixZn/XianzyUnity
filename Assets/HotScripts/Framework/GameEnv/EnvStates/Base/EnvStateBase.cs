namespace HotUpdate
{

    public partial class EnvStateID
    {
    }
    
    public class EnvStateBase
    {
        protected EnvStateManager _stateMgr;
        protected string _nextStateID;
        public string StateID { get; private set; }
        
        private int _gatedReconnectTimerId = 0; //为啥这里能有这个？待干掉

        public virtual void Init(EnvStateManager stateMgr, string stateID)
        {
            _stateMgr = stateMgr;
            StateID = stateID;
            _nextStateID = null;
        }

        /// <summary>
        /// 主要讲解work的含义
        /// 这部分work是上一个状态无法处理的工作，总结出来以后，交付给下一个状态用的
        /// 举例：从GameState切换到BattleState，要先显示Loading（旋转的镂空界面），这个时候不能把主界面关闭，需要等待资源加载完成。
        /// </summary>
        /// <param name="work"></param>
        public virtual void Enter(EnvTransferWorks work)
        {
        }
        


        public virtual void Leave()
        {
            _nextStateID = null;
        }

        /// <summary>
        /// 处理如果从当前状态直接销毁了要做的事儿
        /// </summary>
        public virtual void OnDestroy()
        {
            Leave();
        }
        
        public virtual void Update(float dt)
        {
        
        }
        
        public virtual void LateUpdate()
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

        public virtual EnvTransferWorks CreateTransferWork()
        {
            return null;
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
