

namespace HotUpdate.CoreGame
{
    public interface ILogicConfigContainer
    {
        string ContainerName { get; }
        CustomLogicCfg GetCustomLogicCfg(int id);
    }


}