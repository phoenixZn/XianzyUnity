namespace Xease.CoreGame
{
    //CustomLogic 带有外部约定性质的Key便捷记录在这里。通常是：
    //  1、初始化逻辑时，就约好的黑板值。
    //  2、节点约定好占有的 Key。
    //  注意： 纯内部逻辑的黑板临时变量，不必刻意记录在这 ！！！
    public partial class CvKey
    {
        //通用
        public const string CV_WorldInfo = "CV_WorldInfo";
        public const string CV_LogicWorld = "CV_LogicWorld";
        public const string CV_MetaWorld = "CV_MetaWorld";
        public const string CV_OwnerPlayerInfo = "CV_OwnerPlayerInfo";
        public const string CV_OwnerEntity = "CV_OwnerEntity";


        //主状态机
        public const string CV_BornLogicIdList = "CV_BornLogicIdList";  //出生逻辑列表
        public const string CV_BornBuffIdList = "CV_BornBuffIdList";  //出生Buff列表
        public const string CV_DeathFx = "CV_DeathFx";
        public const string CV_DeathSound = "CV_DeathSound";

        //模式、关卡
        public const string CV_LevelCfgTid = "CV_LevelCfgTid";    //关卡TID
        public const string CV_GameOverReason = "CV_GameOverReason";    //游戏结束原因
        public const string CV_HeroEntityID = "CV_HeroEntityID";
        public const string CV_HeroEntityRef = "CV_HeroEntityRef";

        public const string CV_GameModeCurState = "CV_GameModeCurState"; // 模式当前状态
        public const string CV_ExpCfg = "CV_ExpCfg"; // 模式经验数值配置

        //玩家
        public const string CV_TowerTid = "CV_TowerTid";
        public const string CV_BattleUnitTid = "CV_BattleUnitTid";
        public const string CV_AttributeValuef = "CV_AttributeValuef";
        public const string CV_AttributeKey = "CV_AttributeKey";

        
        //技能
        public const string CV_SkillTid = "CV_SkillTid";
        public const string CV_SkillDmageRate = "CV_SkillDmageRate";
        public const string CV_DamageType = "CV_DamageType";
        public const string CV_SkillLevel = "CV_SkillLevel";
        public const string CV_SpawnSbjTid = "CV_SpawnSbjTid";
        public const string CV_SpawnSbjCount = "CV_SpawnSbjCount";
        public const string CV_SearchRange = "CV_SearchRange";
        public const string CV_TargetEid = "CV_TargetEid";
        public const string CV_TargetPos = "CV_TargetPos";
        public const string CV_TargetDir = "CV_TargetDir";
        public const string CV_HitWhiteList = "CV_HitWhiteList";


        // 子物体
        public const string CV_SbjMoveSpeed = "CV_SbjMoveSpeed";
        public const string CV_SbjTargetPos = "CV_SbjTargetPos"; // 子物体生成时的目标位置
        public const string CV_SbjInitPos = "CV_SbjInitPos"; // 子物体生成时的初始位置
        public const string CV_SbjHitFxRes = "CV_SbjHitFxRes"; // 子物体碰撞特效
        public const string CV_SbjDuration = "CV_SbjDuration"; // 持续时间
        public const string CV_SpawnSobjListOnHit = "CV_SpawnSobjListOnHit";    //子物体碰撞产生子物体
        public const string CV_BuffListOnHit = "CV_BuffListOnHit";  //子物体碰撞，给被碰者附加Buff


        // Buff
        public const string CV_BuffDuration = "CV_BuffDuration"; // Buff持续时间
        public const string CV_CurBuff = "CV_CurBuff"; // 当前buff
        public const string CV_BuffMaxLevel = "CV_BuffMaxLevel";  //buff最大等级
        public const string CV_DisperseBuffList = "CV_DisperseBuffList"; //驱散buff列表
    }
}
