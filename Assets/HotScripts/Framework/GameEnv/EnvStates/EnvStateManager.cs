using System.Collections.Generic;

namespace HotUpdate
{
    public delegate void EnvLogAction(string info);
    
    // 一个特殊的状态机，控制整个ENV的主流程。
    // 特殊之处在于：状态切换时，前后状态会enter，leave接口中刻意感知到对方的存在 （用于做一些交接的手续，和旧物的处理）
    // 当前状态只有一个，如果有需要延迟处理的leave事务，可以交给下一个状态代为处理
    // eg: StateA: "这些我打算清理掉了，你要么？不要我就动手了！“,  StateB : “我看看，哦，这个资源我有用，我拿走了，回头我处理就行，其余你接着处理吧” 

    public partial class EnvStateManager
    {
        protected Dictionary<string, EnvStateBase> _states = new();
        
        protected EnvStateBase _currentStateRef;
        public EnvStateBase CurrentStateRef
        {
            get { return _currentStateRef; }
        }

        public string CurrentStateID
        {
            get
            {
                if (_currentStateRef == null)
                    return null;
                return _currentStateRef.StateID;
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// 外部定义Log方式
        private static void EmptyLog(string info) { }
        public EnvLogAction ErrorLog { get; protected set; }
        public EnvLogAction DevLog { get; protected set; }

        
        //////////////////////////////////////////////////////////////////////////
        public EnvStateManager(EnvLogAction errorLog, EnvLogAction devLog)
        {
            ErrorLog = errorLog ?? EmptyLog;
            DevLog = devLog ?? EmptyLog;
        }
        
        public void Initialize(Dictionary<string, EnvStateBase> states, string dfaultStateID)
        {
            _states = states;
            if (_currentStateRef != null)
            {
                ErrorLog("EnvStateManager.Initialize _currentStateRef != null");
                _currentStateRef = null;
            }
            
            if (states.ContainsKey(dfaultStateID))
            {
                ChangeEnvState(dfaultStateID);
            }
            else
            {
                ErrorLog($"EnvStateManager.Initialize states not contains dfaultState: {dfaultStateID}");
            }
        }

        public void Destroy()
        {
            if (_currentStateRef != null)
            {
                _currentStateRef.Leave();
                _currentStateRef.OnDestroy();
            }
            _currentStateRef = null;
        }

        //////////////////////////////////////////////////////////////////////////
        public virtual void Update(float dt)
        {
            if (_states.Count == 0)
                return;
            
            if (_currentStateRef == null)
                return;
            
            var lastID = _currentStateRef.StateID;
            var nextID = _currentStateRef.CheckTransitions();
            if (nextID == lastID)
            {
                return;
            }

            var next = FindState(nextID);
            if (next != null)
            {
                var ctx = _currentStateRef.CreateTransferWork();
                _currentStateRef.Leave();
                var last = _currentStateRef;
                _currentStateRef = next;
                _currentStateRef.Enter(ctx);
            }

            _currentStateRef.Update(dt);
        }

        public virtual void LateUpdate()
        {
            if (_currentStateRef == null)
            {
                return;
            }

            _currentStateRef.LateUpdate();
        }

        //////////////////////////////////////////////////////////////////////////

        public void ChangeEnvState(string goalStateID)
        {
            DevLog($"Core EnvStateManager.ChangeEnvState goalEnvState={goalStateID}");
            
            if (goalStateID == null)
                return;
            
            var goalState = FindState(goalStateID);
            if (goalState == null)
            {
                ErrorLog($"EnvStateManager TransToState mGoalState == null  goalStateID={goalStateID}");
                return;
            }

            var lastState = _currentStateRef;
            EnvTransferWorks transferWorks = null;
            if (_currentStateRef != null)
            {
                if (goalStateID == _currentStateRef.StateID)
                {
                    return;
                }

                if (_currentStateRef.CantInterrupt())
                {
                    return;
                }

                transferWorks = _currentStateRef.CreateTransferWork();
                _currentStateRef.Leave();
            }

            _currentStateRef = goalState;
            _currentStateRef.Enter(transferWorks);
        }

        private EnvStateBase FindState(string stateID)
        {
            if (stateID == null)
            {
                return null;
            }

            if (_states.TryGetValue(stateID, out var state))
            {
                return state;
            }

            return null;
        }

        //////////////////////////////////////////////////////////////////////////
        /// 跨状态的黑板标识
        ///临时实验 
        protected object Param;

        public object TakeAwayParam()
        {
            var ob = Param;
            Param = null;
            return ob;
        }

        public void SetParam(object param)
        {
            Param = param;
        }
    }
}