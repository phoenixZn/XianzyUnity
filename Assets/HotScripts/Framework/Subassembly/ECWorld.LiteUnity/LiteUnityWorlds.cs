using System;
using System.Collections.Generic;
using Entitas;

namespace Xease.CoreGame
{
    public class LiteUnityWorlds : ECWorlds, IUnityStyleDriver
    {
        protected UnityStyleSystems _rootSystemUnity;
        protected bool _needVerifyRequiredSystemOrder = true;
        
        //////////////////////////////////////////////////////////////////////////
        /// 驱动 Ex:
        public virtual void FixedUpdate(float fdt, float fdt_unscaled)
        {
            _rootSystemUnity?.FixedUpdate(fdt, fdt_unscaled);
        }
        
        public virtual void Update(float dt, float dt_unscaled)
        {
            _rootSystemUnity?.Update(dt, dt_unscaled);
        }

        public virtual void LateUpdate(float dt, float dt_unscaled)
        {
            _rootSystemUnity?.LateUpdate(dt, dt_unscaled);
        }

        public virtual void OnGizmos()
        {
            _rootSystemUnity?.OnGizmos();
        }

        public virtual void OnGUI()
        {
            _rootSystemUnity?.OnGUI();
        }

        //////////////////////////////////////////////////////////////////////////
        /// ECWorlds：
        protected override void CreateSystems()
        {
            base.CreateSystems();
            _rootSystemUnity = _rootSystem as UnityStyleSystems;
            
            //基准系统校验
            if (_needVerifyRequiredSystemOrder)
            {
                Type[] requiredTypes = new Type[]
                {
                    typeof(SysInitializeBasePack),
                    typeof(SysInitializeLiteUnityPack),
                    typeof(SysCommandSend),
                    typeof(SysCommandReceive),
                    typeof(SysGameModeUpdate),
                    typeof(SysViewLoader),
                    typeof(SysSyncViewTransform),
                };
                VerifyRequiredSystemOrder(requiredTypes);
            }
        }

        //////////////////////////////////////////////////////////////////////////
        /// This：
        protected virtual void VerifyRequiredSystemOrder(Type[] requiredTypes)
        {
            if (_rootSystem == null)
            {
                WLogger.LogError("[基准系统检查:LiteUnityWorlds] _rootSystem == null");
                return;
            }
            if (_rootSystemUnity == null)
            {
                WLogger.LogError("[基准系统检查:LiteUnityWorlds] _rootSystemUnity == null");
                return;
            }
            
            var systems = _rootSystemUnity.Systems;
            var lastIndex = -1;

            foreach (var requiredType in requiredTypes)
            {
                var index = FindFirstSystemIndex(systems, requiredType);
                if (index < 0)
                {
                    WLogger.LogError($"[基准系统检查:LiteUnityWorlds] 缺少必需系统: {requiredType.Name}");
                    continue;
                }
                if (index <= lastIndex)
                {
                    WLogger.LogError(
                        $"[基准系统检查:LiteUnityWorlds] 系统顺序错误: {requiredType.Name} 应在上一基准系统之后 (index={index}, prev={lastIndex})");
                }
                lastIndex = index;
            }
        }

        static int FindFirstSystemIndex(List<ISystem> list, Type requiredType)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (requiredType.IsAssignableFrom(list[i].GetType()))
                {
                    return i;
                }
            }
            return -1;
        }
        
    }
}