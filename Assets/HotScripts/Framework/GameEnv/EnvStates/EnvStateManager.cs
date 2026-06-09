using System.Collections.Generic;

namespace Xease
{
    public delegate void EnvLogAction(string info);
    
    // 一个特殊的状态机，控制整个ENV的主流程。
    // 特殊之处在于：状态切换时，前后状态会enter，leave接口中刻意感知到对方的存在 （用于做一些交接的手续，和旧物的处理）
    // 当前状态只有一个，如果有需要延迟处理的leave事务，可以交给下一个状态代为处理
    // eg: StateA: "这些我打算清理掉了，你要么？不要我就动手了！“,  StateB : “我看看，哦，这个资源我有用，我拿走了，回头我处理就行，其余你接着处理吧” 

    public partial class EnvStateManager : IEnvUpdate
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

        public EnvDriver OuterDriver { get; } = new EnvDriver("EnvStates");
        
        
        //////////////////////////////////////////////////////////////////////////
        public EnvStateManager()
        {
        }
        
        public void Initialize(Dictionary<string, EnvStateBase> states, string dfaultStateID)
        {
            _states = states;
            foreach (var kv in states)
            {
                kv.Value.Init(this, kv.Key);
            }
            if (_currentStateRef != null)
            {
                G.LogError("EnvStateManager.Initialize _currentStateRef != null");
                _currentStateRef = null;
            }

            OuterDriver.BindEnvActions(this);
            
            if (states.ContainsKey(dfaultStateID))
            {
                ChangeEnvState(dfaultStateID);
            }
            else
            {
                G.LogError($"EnvStateManager.Initialize states not contains dfaultState: {dfaultStateID}");
            }
        }

        public void Destroy()
        {
            if (_currentStateRef != null)
            {
                UnbindCurrentState(_currentStateRef);
                _currentStateRef.Leave(null);
                _currentStateRef.OnDestroy();
            }
            _currentStateRef = null;

            OuterDriver.UnBindEnvActions(this);
            OuterDriver.ClearAllBind();
        }

        //////////////////////////////////////////////////////////////////////////
        public virtual void EnvUpdate(float dt, float dt_unscaled)
        {
            if (_states.Count == 0)
                return;
            
            if (_currentStateRef == null)
                return;
            
            var stateID = _currentStateRef.StateID;
            var nextStateID = _currentStateRef.CheckTransitions();
            if (nextStateID == stateID)
            {
                return;
            }

            var nextState = FindState(nextStateID);
            if (nextState != null)
            {
                UnbindCurrentState(_currentStateRef);
                _currentStateRef.Leave(nextState);
                var lastState = _currentStateRef;
                _currentStateRef = nextState;
                _currentStateRef.Enter(lastState);
                BindCurrentState(_currentStateRef);
            }
        }

        //////////////////////////////////////////////////////////////////////////

        public void ChangeEnvState(string nextStateID)
        {
            G.Log($"Core EnvStateManager.ChangeEnvState goalEnvState={nextStateID}");
            
            if (nextStateID == null)
                return;
            
            var nextState = FindState(nextStateID);
            if (nextState == null)
            {
                G.LogError($"EnvStateManager TransToState nextState == null  nextStateID={nextStateID}");
                return;
            }

            var lastState = _currentStateRef;
            if (_currentStateRef != null)
            {
                if (nextStateID == _currentStateRef.StateID)
                {
                    return;
                }

                if (_currentStateRef.CantInterrupt())
                {
                    return;
                }
                UnbindCurrentState(_currentStateRef);
                _currentStateRef.Leave(nextState);
            }

            _currentStateRef = nextState;
            _currentStateRef.Enter(lastState);
            BindCurrentState(_currentStateRef);
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

        private void BindCurrentState(EnvStateBase state)
        {
            if (state != null)
                OuterDriver.BindEnvActions(state);
        }

        private void UnbindCurrentState(EnvStateBase state)
        {
            if (state != null)
                OuterDriver.UnBindEnvActions(state);
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
