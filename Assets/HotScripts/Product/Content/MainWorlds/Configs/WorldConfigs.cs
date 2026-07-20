using System;
using System.Collections.Generic;
using Xease.CoreGame;

namespace Xease.CoreGame
{

    public partial class WorldsConfig
    {
        private static readonly Dictionary<string, Func<WorldCreationInfo>> _configs = new();

        static WorldsConfig()
        {
            InitConfigs();
        }

        protected static void AddConfig(string worldCfgID, Func<WorldCreationInfo> factory)
        {
            _configs.Add(worldCfgID, factory);
        }

        public static WorldCreationInfo Get(string worldCfgID)
        {
            if (!_configs.TryGetValue(worldCfgID, out var factory))
            {
                G.LogError($"WorldsConfig.Get: key not found: {worldCfgID}");
                return null;
            }
            return factory();
        }
    }
}
