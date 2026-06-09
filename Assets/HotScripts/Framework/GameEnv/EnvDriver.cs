using System;

namespace Xease
{
    //////////////////////////////////////////////////////////////////////////
    /// 游戏环境定义的标准驱动行为:
    public interface IEnvFixedUpdate
    {
        public void EnvFixedUpdate(float dt, float dt_unscaled);
    }

    public interface IEnvUpdate
    {
        public void EnvUpdate(float dt, float dt_unscaled);
    }

    public interface IEnvLateUpdate
    {
        public void EnvLateUpdate(float dt, float dt_unscaled);
    }

    public interface IEnvAppPause
    {
        public void OnEnvApplicationPause(bool pause);
    }

    public interface IEnvAppFocus
    {
        public void OnEnvApplicationFocus(bool focus);
    }

    public interface IEnvDrawGizmos
    {
        public void EnvDrawGizmos();
    }
    
    public interface IEnvOnGUI
    {
        public void OnEnvGUI();
    }
    
    //////////////////////////////////////////////////////////////////////////
    /// 整合 EnvDriver:
    public class EnvDriver : IEnvFixedUpdate, IEnvUpdate, IEnvLateUpdate, IEnvAppPause, IEnvAppFocus, IEnvDrawGizmos, IEnvOnGUI
    {
        protected Action<float, float> ActionFixedUpdate;
        protected Action<float, float> ActionUpdate;
        protected Action<float, float> ActionLateUpdate;
        protected Action<bool> ApplicationPause;
        protected Action<bool> ApplicationFocus;
        protected Action ActionOnDrawGizmos;
        protected Action ActionOnGUI;

        public string GroupName { get; protected set; }

        public EnvDriver(string groupName)
        {
            GroupName = groupName;
        }

        public void BindEnvActions(object obj)
        {
            if (obj == null)
                return;
            if (obj is IEnvFixedUpdate fixedUpdate)
                ActionFixedUpdate += fixedUpdate.EnvFixedUpdate;
            if (obj is IEnvUpdate update)
                ActionUpdate += update.EnvUpdate;
            if (obj is IEnvLateUpdate lateUpdate)
                ActionLateUpdate += lateUpdate.EnvLateUpdate;
            if (obj is IEnvAppPause applicationPause)
                ApplicationPause += applicationPause.OnEnvApplicationPause;
            if (obj is IEnvAppFocus applicationFocus)
                ApplicationFocus += applicationFocus.OnEnvApplicationFocus;
            if (obj is IEnvDrawGizmos drawGizmos)
                ActionOnDrawGizmos += drawGizmos.EnvDrawGizmos;
            if (obj is IEnvOnGUI onGUI)
                ActionOnGUI += onGUI.OnEnvGUI;
        }
        
        public void UnBindEnvActions(object obj)
        {
            if (obj is IEnvFixedUpdate fixedUpdate)
                ActionFixedUpdate -= fixedUpdate.EnvFixedUpdate;
            if (obj is IEnvUpdate update)
                ActionUpdate -= update.EnvUpdate;
            if (obj is IEnvLateUpdate lateUpdate)
                ActionLateUpdate -= lateUpdate.EnvLateUpdate;
            if (obj is IEnvAppPause applicationPause)
                ApplicationPause -= applicationPause.OnEnvApplicationPause;
            if (obj is IEnvAppFocus applicationFocus)
                ApplicationFocus -= applicationFocus.OnEnvApplicationFocus;
            if (obj is IEnvDrawGizmos drawGizmos)
                ActionOnDrawGizmos -= drawGizmos.EnvDrawGizmos;
            if (obj is IEnvOnGUI onGUI)
                ActionOnGUI -= onGUI.OnEnvGUI;
        }

        public void ClearAllBind()
        {
            ActionFixedUpdate = null;
            ActionUpdate = null;
            ActionLateUpdate = null;
            ApplicationPause = null;
            ApplicationFocus = null;
            ActionOnDrawGizmos = null;
            ActionOnGUI = null;
        }

        //////////////////////////////////////////////////////////////////////////
        public void EnvFixedUpdate(float dt, float dt_unscaled)
        {
            ActionFixedUpdate?.Invoke(dt, dt_unscaled);
        }

        public void EnvUpdate(float dt, float dt_unscaled)
        {
            ActionUpdate?.Invoke(dt, dt_unscaled);
        }

        public void EnvLateUpdate(float dt, float dt_unscaled)
        {
            ActionLateUpdate?.Invoke(dt, dt_unscaled);
        }

        public void EnvDrawGizmos()
        {
            ActionOnDrawGizmos?.Invoke();
        }
        
        public void OnEnvApplicationPause(bool pause)
        {
            ApplicationPause?.Invoke(pause);
        }
        
        public void OnEnvApplicationFocus(bool focus)
        {
            ApplicationFocus?.Invoke(focus);
        }

        public void OnEnvGUI()
        {
            ActionOnGUI?.Invoke();
        }
        
    }
}