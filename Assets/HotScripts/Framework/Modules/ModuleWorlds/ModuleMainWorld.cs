using System;
using Xease.CoreGame;

namespace Xease
{
    public partial class ModuleMainWorld : Module, IEnvUpdate, IEnvFixedUpdate, IEnvLateUpdate, IEnvDrawGizmos, IEnvOnGUI
    {
        protected ECWorlds _mainWorld;
        protected IUnityStyleDriver _worldDriverEx;
        public ECWorlds MainWorld => _mainWorld;
        public bool IsActive { get; private set; }
        
        public ModuleMainWorld()
        {
        }
        
        protected override void OnInit()
        {
            G.Log("ModuleMainWorld 构造");
            base.OnInit();
        }
        
        protected override void OnShutdown()
        {
            DestroyGameWorld();
            base.OnShutdown();
        }
        
        //////////////////////////////////////////////////////////////////////////
        public void SetActive(bool active)
        {
            IsActive = active;
        }

        public void EnvUpdate(float dt, float dt_unscaled)
        {
            if (!IsActive)
                return;
            if (_mainWorld == null)
                return;
            _worldDriverEx?.Update(dt, dt_unscaled);
            _mainWorld?.Execute();
        }

        public void EnvFixedUpdate(float dt, float dt_unscaled)
        {
            if (!IsActive)
                return;
            _worldDriverEx?.FixedUpdate(dt, dt_unscaled);
        }

        public void EnvLateUpdate(float dt, float dt_unscaled)
        {
            if (!IsActive)
                return;
            _worldDriverEx?.LateUpdate(dt, dt_unscaled);
        }

        public void EnvDrawGizmos()
        {
            if (!IsActive)
                return;
            _worldDriverEx?.OnGizmos();
        }

        public void OnEnvGUI()
        {
            if (!IsActive)
                return;
            _worldDriverEx?.OnGUI();
        }

        //////////////////////////////////////////////////////////////////////////
        public void CreateGameWorld<T>(WorldCreationInfo creationInfo) where T : ECWorlds
        {
            if (_mainWorld != null)
            {
                G.LogError("CreateGameWorld 发现已有 _mainWorld != null, 强制执行清理");
                DestroyGameWorld();    
            }
            _mainWorld = Activator.CreateInstance(typeof(T)) as T;
            _worldDriverEx = _mainWorld as IUnityStyleDriver;
            _mainWorld.InitWorlds(creationInfo);
        }

        public void DestroyGameWorld()
        {
            if (_mainWorld == null)
                return;

            _mainWorld.DestroyWorlds();
            _mainWorld = null;
            _worldDriverEx = null;
            IsActive = false;
        }
    }
}