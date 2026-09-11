using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Xease.Audio
{
    public partial class AudioManager
    {
        private readonly Dictionary<string, Type> _eventTypes = new();
        private void InitAttr()
        {
            CollectAttr();
        }
        
        private void CollectAttr()
        {
            _eventTypes.Clear();
            var classes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(type => type.GetCustomAttributes<AudioEventAttr>().Any());
            foreach (var @class in classes)
            {
                _eventTypes.Add(@class.Name, @class);
            }
        }

        private Type GetEventType(string typeName)
        {
            if (_eventTypes.TryGetValue(typeName, out var targetType))
            {
                return targetType;
            }
            Audio.LogWarning($"[Audio] Attribute of AudioEvent \"{typeName}\" not found");
            return null;
        }

        public List<string> GetAllEventTypes()
        {
            if (_eventTypes.Count == 0)
            {
                CollectAttr();
            }
            return _eventTypes.Keys.ToList();
        }
    }
}