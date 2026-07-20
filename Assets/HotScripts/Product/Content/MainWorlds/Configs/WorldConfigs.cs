using System;
using System.Collections.Generic;
using Xease.CoreGame;

namespace Xease.CoreGame
{

    public partial class WorldsConfig
    {
        private readonly Dictionary<string, Func<WorldCreationInfo>> _configs = new();

        public WorldsConfig()
        {
            InitConfigs_Main();
        }

        protected void AddConfig(string worldCfgID, Func<WorldCreationInfo> factory)
        {
            _configs.Add(worldCfgID, factory);
        }

        public WorldCreationInfo Get(string worldCfgID)
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
