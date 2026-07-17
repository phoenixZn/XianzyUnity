using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public interface IFixedUpdateSystem
    {
        void FixedUpdate(float fdt, float fdt_unscaled);
    }

    public interface IUpdateSystem
    {
        void Update(float dt, float dt_unscaled);
    }

    public interface ILateUpdateSystem
    {
        void LateUpdate(float dt, float dt_unscaled);
    }

    public interface IGizmosSystem
    {
        void OnGizmos();
    }
    public interface IUnityGUI
    {
        void OnGUI();
    }
     
    
    public interface IUnityStyleDriver : IFixedUpdateSystem, IUpdateSystem, ILateUpdateSystem, IGizmosSystem, IUnityGUI
    {
    }

    //////////////////////////////////////////////////////////////////////////
    public class UnityStyleSystems : Systems, IUnityStyleDriver
    {
        private readonly List<ISystem> _systems = new ();
        private readonly List<IFixedUpdateSystem> _fixedUpdateSystemList = new ();
        private readonly List<IUpdateSystem> _updateSystemList = new ();
        private readonly List<ILateUpdateSystem> _lateUpdateSystemList = new ();
        private readonly List<IGizmosSystem> _gizmosSystemList = new ();
        private readonly List<IUnityGUI> _unityGUISystemList = new ();

        public List<ISystem> Systems => _systems;
        
        public override Systems Add(ISystem system)
        {
            _systems.Add(system);

            if (system is IFixedUpdateSystem _fixedUpdateSystem)
            {
                _fixedUpdateSystemList.Add(_fixedUpdateSystem);
            }
            
            if (system is IUpdateSystem updateSystemList)
            {
                _updateSystemList.Add(updateSystemList);
            }
            
            if (system is ILateUpdateSystem lateUpdateSystem)
            {
                _lateUpdateSystemList.Add(lateUpdateSystem);
            }
            
            if (system is IGizmosSystem drawGizmosSystem)
            {
                _gizmosSystemList.Add(drawGizmosSystem);
            }
            
            if (system is IUnityGUI unityGUISystem)
            {
                _unityGUISystemList.Add(unityGUISystem);
            }
            
            return base.Add(system);
        }

        public void FixedUpdate(float fdt, float fdt_unscaled)
        {
            foreach (var item in _fixedUpdateSystemList)
            {
                item.FixedUpdate(fdt, fdt_unscaled);
            }
        }
        
        public void Update(float dt, float dt_unscaled)
        {
            foreach (var item in _updateSystemList)
            {
                item.Update(dt, dt_unscaled);
            }
        }

        public void LateUpdate(float dt, float dt_unscaled)
        {
            foreach (var item in _lateUpdateSystemList)
            {
                item.LateUpdate(dt, dt_unscaled);
            }
        }

        public void OnGizmos()
        {
            foreach (var item in _gizmosSystemList)
            {
                item.OnGizmos();
            }
        }

        public void OnGUI()
        {
            foreach (var item in _unityGUISystemList)
            {
                item.OnGUI();
            }
        }
    }
}