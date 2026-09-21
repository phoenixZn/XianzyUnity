namespace Xease.Engine.Core
{
    public interface IConsoleCommand
    {
        string Name { get; }
        string Description { get; }
        bool Execute(string[] args);
    }
}
