using System;
using System.Collections.Generic;

namespace Xease
{
    /// <summary>
    /// 模块数据的生命周期
    /// </summary>
    public enum ModuleDataType
    {
        /// <summary>
        /// 全局数据，仅在模块关闭时清理
        /// </summary>
        Global,
        /// <summary>
        /// 玩家数据，当玩家登出时清理
        /// </summary>
        Player,
        /// <summary>
        /// 场景数据，当离开场景时清理
        /// </summary>
        Scene,
    }
    
    public partial class ModuleManager
    {
        private enum State : byte
        {
            Shutdown,
            Inited,
            Started,
        }
        
        protected List<IModule> moduleList = new();
        protected Dictionary<Type, IModule> moduleDict = new();
        private State _state = State.Shutdown;
        
        private bool IsInited => _state >= State.Inited;
        private bool IsStarted => _state >= State.Started;
        private bool IsShutdown => _state == State.Shutdown;

        public EnvDriver OuterDriver { get; } = new EnvDriver("Modules");

        public ModuleManager()
        {
        }

        /// <summary>
        /// 获取指定类型模块实例
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        /// <returns>指定类型模块实例</returns>
        public T GetModule<T>() where T : class, IModule
        {
            return moduleDict.TryGetValue(typeof(T), out var module) ? module as T : null;
        }


        //////////////////////////////////////////////////////////////////////////
        public void Register(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (!typeof(IModule).IsAssignableFrom(type))
                {
                    G.LogError($"[ModuleManager] {type} does not implement IModule");
                    continue;
                }
                
                if (moduleDict.ContainsKey(type))
                {
                    G.LogError($"[ModuleManager] {type} is already registered");
                    continue;
                }

                if (Activator.CreateInstance(type) is not IModule module)
                {
                    G.LogError($"[ModuleManager] Failed to create instance of {type}");
                    continue;
                }
                
                RegisterModule(module);
            }
        }

        /// <summary>
        /// 批量注册模块实例
        /// </summary>
        /// <param name="modules">模块实例列表</param>
        public void Register(IEnumerable<IModule> modules)
        {
            foreach (var module in modules)
            {
                Register(module);
            }
        }

        /// <summary>
        /// 注册模块实例
        /// </summary>
        /// <param name="module">模块实例</param>
        public void Register(IModule module)
        {
            RegisterModule(module);
        }

        /// <summary>
        /// 根据类型创建模块实例并注册
        /// </summary>
        /// <typeparam name="T">模块类型</typeparam>
        public void Register<T>() where T : class, IModule, new()
        {
            if (moduleDict.ContainsKey(typeof(T)))
            {
                G.LogError($"[ModuleManager] {typeof(T)} is already registered");
                return;
            }

            T module = new();
            RegisterModule(module);
        }

        private void RegisterModule(IModule module)
        {
            var type = module.GetType();
            if (moduleDict.ContainsKey(type))
            {
                G.LogError($"[ModuleManager] {type} is already registered");
                return;
            }

            moduleList.Add(module);
            moduleDict.Add(type, module);

            if (IsInited)
            {
                module.Init();
            }
            if (IsStarted)
            {
                module.Start();
            }

            OuterDriver.BindEnvActions(module);
        }

        public void Unregister(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                Unregister(type);
            }
        }

        public void Unregister(Type type)
        {
            UnregisterModule(type);
        }

        public void Unregister<T>() where T : class, IModule
        {
            UnregisterModule(typeof(T));
        }

        private void UnregisterModule(Type type)
        {
            if (!moduleDict.TryGetValue(type, out var module))
            {
                G.LogWarning($"[ModuleManager] {type} does not registered");
                return;
            }

            OuterDriver.UnBindEnvActions(module);

            if (IsInited)
            {
                module.Shutdown();
            }

            moduleList.Remove(module);
            moduleDict.Remove(type);
        }
        
        
        public void Init(IEnumerable<Type> types)
        {
            if (_state != State.Shutdown)
                return;
            
            if(types is not null)
                Register(types);

            Broadcast(m =>
            {
                m.Init();
            });

            _state = State.Inited;
        }

        public void Start()
        {
            if (_state != State.Inited)
                return;
            
            Broadcast(m =>
            {
                m.Start();
            });

            _state = State.Started;
        }

        public void Shutdown()
        {
            if (IsShutdown)
                return;

            for (int i = 0; i < moduleList.Count; ++i)
            {
                OuterDriver.UnBindEnvActions(moduleList[i]);
            }
            OuterDriver.ClearAllBind();

            Broadcast(m =>
            {
                m.Shutdown();
            }, true);

            moduleList.Clear();
            moduleDict.Clear();

            _state = State.Shutdown;
        }
        

        /// <summary>
        /// 模块广播
        /// </summary>
        /// <param name="action">广播委托行为</param>
        /// <param name="reverse">是否按注册顺序倒序执行，默认为否，即顺序执行</param>
        public void Broadcast(Action<IModule> action, bool reverse = false)
        {
            if (reverse)
            {
                for (int i = moduleList.Count - 1; i >= 0; --i)
                {
                    try
                    {
                        action(moduleList[i]);
                    }
                    catch (Exception ex)
                    {
                        G.LogError($"[ModuleManager] Module[{i}] type: {moduleList[i].GetType()} callback exception: {ex}");
                    }
                }
            }
            else
            {
                for (int i = 0; i < moduleList.Count; ++i)
                {
                    try
                    {
                        action(moduleList[i]);
                    }
                    catch (Exception ex)
                    {
                        G.LogError($"[ModuleManager] Module[{i}] type: {moduleList[i].GetType()} callback exception: {ex}");
                    }
                }
            }
        }

        /// <summary>
        /// 带传参的模块广播
        /// </summary>
        /// <param name="action">广播委托行为</param>
        /// <param name="arg"></param>
        /// <param name="reverse">是否按注册顺序倒序执行，默认为否，即顺序执行</param>
        /// <typeparam name="T">传参类型</typeparam>
        public void Broadcast<T>(Action<IModule, T> action, T arg, bool reverse = false)
        {
            if (reverse)
            {
                for (int i = moduleList.Count - 1; i >= 0; --i)
                {
                    try
                    {
                        action(moduleList[i], arg);
                    }
                    catch (Exception ex)
                    {
                        G.LogError($"[ModuleManager] Module type: {moduleList[i].GetType()} callback exception: {ex}");
                    }
                }
            }
            else
            {
                for (int i = 0; i < moduleList.Count; ++i)
                {
                    try
                    {
                        action(moduleList[i], arg);
                    }
                    catch (Exception ex)
                    {
                        G.LogError($"[ModuleManager] Module type: {moduleList[i].GetType()} callback exception: {ex}");
                    }
                }
            }
        }

        
        //////////////////////////////////////////////////////////////////////////
        /// Ex:
        private static readonly Action<IModule> OnPlayerLoginAction = m =>
        {
            m.InitData(ModuleDataType.Player);
            if (m is IPlayerStateModule p)
                p.OnPlayerLogin();
        };

        private static readonly Action<IModule> OnPlayerLogoutAction = m =>
        {
            if (m is IPlayerStateModule p)
                p.OnPlayerLogout();
            m.ClearData(ModuleDataType.Player);
        };

        private static readonly Action<IModule> OnDisconnectedAction = m =>
        {
            if (m is INetStateModule n)
                n.OnDisconnected();
        };

        private static readonly Action<IModule> OnSceneEnteredAction = m =>
        {
            if (m is ISceneStateModule s)
                s.OnSceneEntered();
        };

        private static readonly Action<IModule> OnSceneLeftAction = m =>
        {
            if (m is ISceneStateModule s)
                s.OnSceneLeft();
            m.ClearData(ModuleDataType.Scene);
        };

        private static readonly Action<IModule, bool> OnApplicationPausedAction = (m, paused) =>
        {
            if (m is IApplicationStateModule a)
                a.OnApplicationPaused(paused);
        };
        
        /// <summary>
        /// 玩家登录的广播（顺序）
        /// </summary>
        public void OnPlayerLogin()
        {
            Broadcast(OnPlayerLoginAction);
        }

        /// <summary>
        /// 玩家登出的广播（倒序）
        /// </summary>
        public void OnPlayerLogout()
        {
            Broadcast(OnPlayerLogoutAction, true);
        }
        
        /// <summary>
        /// 进入场景的广播（顺序）
        /// </summary>
        public void OnSceneEntered()
        {
            Broadcast(OnSceneEnteredAction);
        }

        /// <summary>
        /// 离开场景的广播（倒序）
        /// </summary>
        public void OnSceneLeft()
        {
            Broadcast(OnSceneLeftAction, true);
        }

        /// <summary>
        /// 应用暂停/恢复时的广播
        /// 暂停时顺序广播，恢复时倒序
        /// </summary>
        /// <param name="paused"></param>
        public void OnApplicationPaused(bool paused)
        {
            Broadcast(OnApplicationPausedAction, paused, !paused);
        }

    }
    

}