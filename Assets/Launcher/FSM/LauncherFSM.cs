using System.Collections;
using System.Collections.Generic;

namespace Launcher
{
    //启动步骤状态机
    //原则上不依赖于引擎和Unity的接口保持低耦合
    public partial class LauncherFSM : ILauncherContext
    {
        protected Dictionary<string, LauncherState> _states = new();
        protected LauncherState _currentState;
        protected Dictionary<string, object> _blackboard = new();

        
        //////////////////////////////////////////////////////////////////////////
        /// 外部定义Log方式
        private static void EmptyLog(string info) { }
        public LauncherLogAction LogError { get; protected set; }
        public LauncherLogAction LogInfo { get; protected set; }

        public void SetLauncherLog(LauncherLogAction errorLog, LauncherLogAction devLog)
        {
            LogError = errorLog ?? EmptyLog;
            LogInfo = devLog ?? EmptyLog;
        }
        
        
        public LauncherState CurrentState
        {
            get { return _currentState; }
        }

        public string CurrentStateID
        {
            get
            {
                if (_currentState == null)
                    return null;
                return _currentState.StateID;
            }
        }

        public virtual bool AddState(string stateID, LauncherState state)
        {
            if (!_states.ContainsKey(stateID))
            {
                state.Init(this, stateID);
                _states.Add(stateID, state);
                return true;
            }
            return true;
        }

        public virtual void Start(string dfaultStateID)
        {
            ForceChangeState(dfaultStateID);
        }

        public virtual void Update(float dt)
        {
            if (_states.Count == 0)
                return;
            if (_currentState == null)
            {
                return;
            }
            _currentState.Update(dt);
            var lastID = _currentState.StateID;
            var nextID = _currentState.CheckTransitions();
            if (nextID == lastID)
            {
                return;
            }
            var next = FindState(nextID);
            if (next != null)
            {
                _currentState.Leave();
                _currentState = next;
                _currentState.Enter();
            }
        }

        public virtual void ForceChangeState(string nextStateID)
        {
            LogInfo($"LauncherContext ForceChangeState nextStateID={nextStateID}");
            if (nextStateID == null)
                return;
            var goalState = FindState(nextStateID);
            if (goalState == null)
            {
                LogError($"LauncherContext goalState == null  nextStateID={nextStateID}");
                return;
            }
            if (_currentState != null)
            {
                if (nextStateID == _currentState.StateID)
                {
                    return;
                }
                _currentState.Leave();
            }
            _currentState = goalState;
            _currentState.Enter();
        }

        protected virtual LauncherState FindState(string stateID)
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

        /// <summary>
        /// 设置黑板数据
        /// </summary>
        public void SetBlackboardValue(string key, object value)
        {
            _blackboard[key] = value;
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        public object GetBlackboardValue(string key, object defaultValue = null)
        {
            if (_blackboard.TryGetValue(key, out var value))
            {
                return value;
            }
            value = defaultValue;
            return value;
        }

        //////////////////////////////////////////////////////////////////////////
        // UnityEngine 依赖
        public UnityEngine.MonoBehaviour OwnerMonoBhv { get; protected set; }
        
        public void InitByUnityEngine(UnityEngine.MonoBehaviour ownerMono)
        {
            //LauncherFsm在Unity下，被一个MonoBehaviour持有、驱动
            OwnerMonoBhv = ownerMono;
            SetLauncherLog(UnityEngine.Debug.LogError, UnityEngine.Debug.Log);
        }

        public virtual UnityEngine.Coroutine StartCoroutine(IEnumerator routine)
        {
            return OwnerMonoBhv?.StartCoroutine(routine);
        }
    }
}
