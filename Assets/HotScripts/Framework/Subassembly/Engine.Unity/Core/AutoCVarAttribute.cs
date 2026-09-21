using System;

namespace Xease.Engine.Core
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
    public sealed class AutoCVarAttribute : Attribute
    {
        public AutoCVarAttribute(string name, string description = "")
        {
            Name = name;
            Description = description ?? string.Empty;
        }

        public string Name { get; }
        public string Description { get; }
    }
}
