using Xease.CoreGame;

namespace Xease
{
    public partial class ModuleGameWorld : Module, IEnvUpdate, IEnvFixedUpdate, IEnvLateUpdate, IEnvDrawGizmos
    {
        protected LiteUnityWorlds _mainWorld;
        
        public LiteUnityWorlds MainWorld => _mainWorld;
        
        
        public ModuleGameWorld()
        {
        }
        
        protected override void OnInit()
        {
            G.Log("ModuleGameWorld 构造");
            base.OnInit();
        }
        
        protected override void OnShutdown()
        {
            DestroyGameWorld();
            base.OnShutdown();
        }
        
        //////////////////////////////////////////////////////////////////////////
        public void EnvUpdate(float dt, float dt_unscaled)
        {
            if (_mainWorld == null)
                return;

            _mainWorld.Update(dt, dt_unscaled);
            _mainWorld.Execute();
        }

        public void EnvFixedUpdate(float dt, float dt_unscaled)
        {
            if (_mainWorld == null)
                return;

            _mainWorld.FixedUpdate(dt, dt_unscaled);
        }

        public void EnvLateUpdate(float dt, float dt_unscaled)
        {
            if (_mainWorld == null)
                return;

            _mainWorld.LateUpdate(dt, dt_unscaled);
        }

        public void EnvDrawGizmos()
        {
            if (_mainWorld == null)
                return;

            _mainWorld.OnGizmos();
        }

        //////////////////////////////////////////////////////////////////////////
        public void CreateGameWorld(IWorldCreationInfo creationInfo)
        {
            DestroyGameWorld();
            _mainWorld = new LiteUnityWorlds();
            _mainWorld.InitWorlds(creationInfo);
        }

        public void DestroyGameWorld()
        {
            if (_mainWorld == null)
                return;

            _mainWorld.DestroyWorlds();
            _mainWorld = null;
        }
    }
}