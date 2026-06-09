using System.Collections.Generic;

namespace GameLogicToolData
{
    public class TestUnitData
    {
        public int FighterTid = 1403;
        public int UintTid = 101;
        public int Level = 5;
        public int SkillLogic = 0;
        public int FSMLogic = 0;
    }
    
    public class TestGameInitData
    {
        public int LevelCfgTid;
        public int PlayerLevel;

        public int OverrideLevelLogicID;
        public int OverrideModeLogicID;

        //测试数据
        public List<TestUnitData> Units;
    }
    
}
