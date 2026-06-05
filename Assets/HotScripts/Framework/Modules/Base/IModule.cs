
namespace Xease
{
    /// <summary>
    /// 模块基本接口，包含基本生命周期管理和数据清理接口
    /// </summary>
    public interface IModule
    {
        void Init();
        void Start();
        void Shutdown();

        #region 数据层
        void InitData(ModuleDataType type);
        void ClearData(ModuleDataType type);
        #endregion
    }

    /// <summary>
    /// 和玩家状态相关的模块接口
    /// </summary>
    public interface IPlayerStateModule
    {
        void OnPlayerLogin();
        void OnPlayerLogout();
    }

    /// <summary>
    /// 和网络连接状态相关的模块接口
    /// </summary>
    public interface INetStateModule
    {
        void OnDisconnected();
    }

    /// <summary>
    /// 和场景状态相关的模块接口
    /// </summary>
    public interface ISceneStateModule
    {
        void OnSceneEntered();
        void OnSceneLeft();
    }

    /// <summary>
    /// 和应用状态相关的模块接口
    /// </summary>
    public interface IApplicationStateModule
    {
        void OnApplicationPaused(bool paused);
    }

    /// <summary>
    /// 清空模块数据的能力接口
    /// </summary>
    public interface IResettable
    {
        void Reset();
    }
}
