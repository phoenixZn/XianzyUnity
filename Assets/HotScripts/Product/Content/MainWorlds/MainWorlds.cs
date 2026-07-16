using Entitas;

namespace Xease.CoreGame
{
    public class MainWorldCreationInfo : WorldCreationInfo, IGameModeParam
    {
        public MainWorldCreationInfo() : base("Main")
        {
        }
        public int ModeLogicID { get; set; }
    }

    //////////////////////////////////////////////////////////////////////////
    /// 主游戏世界
    public class MainWorlds : LiteUnityWorlds
    {
        protected override void CreateSystems()
        {
            base.CreateSystems();
            _rootSystem.Add(new SysGameplayInitialize_Main(this));
            //systems.Add(new SysDebugCoreGame(this));
            //systems.Add(new SysTimeScale(this));
            
            
            AddUnitTestSystems();
        }

        private void AddUnitTestSystems()
        {
            //单元测试:
            _rootSystem.Add(new UnitTestSystems_Base(this));
        }
    }
}