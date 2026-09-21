using System;

namespace Xease.Engine.Core
{
    public interface IConsoleVariable
    {
        string Name { get; }
        string Description { get; }
        ConsolePriority Priority { get; }
        Type ValueType { get; }
        bool IsRegistered { get; }

        object BoxedValue { get; }
        object BoxedDefaultValue { get; }

        bool SetFromString(string valueText, ConsolePriority priority);
        string GetValueString();
        void BindMetadata(string name, string description);
    }
}
