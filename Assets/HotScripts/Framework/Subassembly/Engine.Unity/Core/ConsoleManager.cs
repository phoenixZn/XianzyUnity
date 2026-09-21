using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Xease.Engine.Core
{
    
    /// <summary>
    /// CVar 值来源优先级，数值越大优先级越高。
    /// </summary>
    public enum ConsolePriority : byte
    {
        Default = 0,
        Code = 10,
        ConfigFile = 20,
        CommandLine = 30,
        Console = 40,
    }
    
    public static class ConsoleManager
    {
        private readonly struct AutoFieldEntry
        {
            public readonly FieldInfo FieldInfo;
            public readonly AutoCVarAttribute Attribute;

            public AutoFieldEntry(FieldInfo fieldInfo, AutoCVarAttribute attribute)
            {
                FieldInfo = fieldInfo;
                Attribute = attribute;
            }
        }

        private static readonly ReaderWriterLockSlim s_CVarLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly ReaderWriterLockSlim s_CommandLock = new ReaderWriterLockSlim(LockRecursionPolicy.NoRecursion);
        private static readonly Dictionary<string, IConsoleVariable> s_CVars = new Dictionary<string, IConsoleVariable>(256, StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, IConsoleCommand> s_Commands = new Dictionary<string, IConsoleCommand>(64, StringComparer.OrdinalIgnoreCase);
        private static readonly object s_AutoScanConfigLock = new object();
        private static AutoFieldEntry[] s_AutoFields;
        private static HashSet<string> s_AutoScanAssemblyWhitelist;

        public static int VariableCount
        {
            get
            {
                s_CVarLock.EnterReadLock();
                try
                {
                    return s_CVars.Count;
                }
                finally
                {
                    s_CVarLock.ExitReadLock();
                }
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            ResetForPlayMode();
            SetAutoScanAssemblyWhitelist(new string[] 
            {
                "Engine.Core",
                "Engine",
                "Unity.RenderPipelines.Universal.Runtime"
            });
            EnsureAutoRegistrationCache();
            RegisterAutoCVars();
            // ApplyCommandLineOverrides(System.Environment.GetCommandLineArgs(), ConsolePriority.CommandLine);
        }

        public static bool RegisterCVar(string name, IConsoleVariable cvar, string description = "")
        {
            if (string.IsNullOrEmpty(name) || cvar == null)
            {
                return false;
            }

            s_CVarLock.EnterWriteLock();
            try
            {
                if (s_CVars.TryGetValue(name, out IConsoleVariable existing) && !ReferenceEquals(existing, cvar))
                {
                    Debug.LogWarning($"[ConsoleManager] Duplicate cvar name: {name}");
                    return false;
                }

                cvar.BindMetadata(name, description);
                s_CVars[name] = cvar;
                return true;
            }
            finally
            {
                s_CVarLock.ExitWriteLock();
            }
        }

        public static bool UnregisterCVar(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            s_CVarLock.EnterWriteLock();
            try
            {
                return s_CVars.Remove(name);
            }
            finally
            {
                s_CVarLock.ExitWriteLock();
            }
        }

        public static IConsoleVariable FindCVar(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return null;
            }

            s_CVarLock.EnterReadLock();
            try
            {
                s_CVars.TryGetValue(name, out IConsoleVariable cvar);
                return cvar;
            }
            finally
            {
                s_CVarLock.ExitReadLock();
            }
        }

        public static bool TrySetCVarFromString(string name, string valueText, ConsolePriority priority = ConsolePriority.Default)
        {
            IConsoleVariable cvar = FindCVar(name);
            if (cvar == null)
            {
                return false;
            }

            return cvar.SetFromString(valueText, priority);
        }

        public static void ForEachCVar(Action<IConsoleVariable> visitor)
        {
            if (visitor == null)
            {
                return;
            }

            s_CVarLock.EnterReadLock();
            try
            {
                foreach (KeyValuePair<string, IConsoleVariable> pair in s_CVars)
                {
                    visitor(pair.Value);
                }
            }
            finally
            {
                s_CVarLock.ExitReadLock();
            }
        }

        public static bool RegisterCommand(IConsoleCommand command)
        {
            if (command == null || string.IsNullOrEmpty(command.Name))
            {
                return false;
            }

            s_CommandLock.EnterWriteLock();
            try
            {
                s_Commands[command.Name] = command;
                return true;
            }
            finally
            {
                s_CommandLock.ExitWriteLock();
            }
        }

        public static bool ExecuteCommand(string commandName, string[] args)
        {
            if (string.IsNullOrEmpty(commandName))
            {
                return false;
            }

            s_CommandLock.EnterReadLock();
            try
            {
                if (!s_Commands.TryGetValue(commandName, out IConsoleCommand command))
                {
                    return false;
                }

                return command.Execute(args ?? Array.Empty<string>());
            }
            finally
            {
                s_CommandLock.ExitReadLock();
            }
        }

        /// <summary>
        /// 配置 AutoCVar 反射扫描的程序集白名单；传 null 或空集合表示扫描全部程序集。
        /// 建议在任何 CVar 访问前（例如首个 RuntimeInitializeOnLoadMethod）尽早调用。
        /// </summary>
        public static void SetAutoScanAssemblyWhitelist(IEnumerable<string> assemblyNames)
        {
            lock (s_AutoScanConfigLock)
            {
                if (assemblyNames == null)
                {
                    s_AutoScanAssemblyWhitelist = null;
                    s_AutoFields = null;
                    return;
                }

                var whitelist = new HashSet<string>(StringComparer.Ordinal);
                foreach (string name in assemblyNames)
                {
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    whitelist.Add(name.Trim());
                }

                s_AutoScanAssemblyWhitelist = whitelist.Count == 0 ? null : whitelist;
                s_AutoFields = null;
            }
        }

        public static void ApplyCommandLineOverrides(string[] args, ConsolePriority priority)
        {
            if (args == null || args.Length == 0)
            {
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (string.IsNullOrEmpty(arg))
                {
                    continue;
                }

                int offset = arg[0] == '+' ? 1 : 0;
                int equalIndex = arg.IndexOf('=');
                if (equalIndex <= offset)
                {
                    continue;
                }

                string key = arg.Substring(offset, equalIndex - offset);
                string value = arg.Substring(equalIndex + 1);
                if (!TrySetCVarFromString(key, value, priority))
                {
                    continue;
                }
            }
        }

        private static void ResetForPlayMode()
        {
            s_CVarLock.EnterWriteLock();
            try
            {
                s_CVars.Clear();
            }
            finally
            {
                s_CVarLock.ExitWriteLock();
            }

            s_CommandLock.EnterWriteLock();
            try
            {
                s_Commands.Clear();
            }
            finally
            {
                s_CommandLock.ExitWriteLock();
            }
        }

        private static void EnsureAutoRegistrationCache()
        {
            if (s_AutoFields != null)
            {
                return;
            }

            List<AutoFieldEntry> list = new List<AutoFieldEntry>(256);
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            int registered = 0;
            for (int i = 0; i < assemblies.Length; i++)
            {
                if (!IsAssemblyAllowed(assemblies[i]))
                {
                    continue;
                }
                Debug.Log($"EnsureAutoRegistrationCache:{i} {assemblies[i].GetName().Name}");

                Type[] types;
                try
                {
                    types = assemblies[i].GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types;
                }

                if (types == null)
                {
                    continue;
                }

                for (int t = 0; t < types.Length; t++)
                {
                    Type type = types[t];
                    if (type == null)
                    {
                        continue;
                    }

                    FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int f = 0; f < fields.Length; f++)
                    {
                        FieldInfo field = fields[f];
                        AutoCVarAttribute attr = field.GetCustomAttribute<AutoCVarAttribute>(false);
                        if (attr == null || !typeof(IConsoleVariable).IsAssignableFrom(field.FieldType))
                        {
                            continue;
                        }

                        list.Add(new AutoFieldEntry(field, attr));
                        registered++;
                    }
                }
            }
            Debug.Log($"EnsureAutoRegistrationCache registered : {registered}");

            s_AutoFields = list.ToArray();
        }

        private static bool IsAssemblyAllowed(Assembly assembly)
        {
            HashSet<string> whitelist = s_AutoScanAssemblyWhitelist;
            if (whitelist == null || whitelist.Count == 0)
            {
                return true;
            }

            return whitelist.Contains(assembly.GetName().Name);
        }

        private static void RegisterAutoCVars()
        {
            AutoFieldEntry[] fields = s_AutoFields;
            if (fields == null || fields.Length == 0)
            {
                return;
            }

            for (int i = 0; i < fields.Length; i++)
            {
                object boxed = fields[i].FieldInfo.GetValue(null);
                if (!(boxed is IConsoleVariable cvar))
                {
                    continue;
                }

                RegisterCVar(fields[i].Attribute.Name, cvar, fields[i].Attribute.Description);
            }
        }
    }
}
