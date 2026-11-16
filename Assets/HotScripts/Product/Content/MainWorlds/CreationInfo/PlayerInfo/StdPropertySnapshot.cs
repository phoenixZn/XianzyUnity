using System;

namespace Xease.CoreGame
{
    public static partial class PropertyFloat
    {
        // 基础属性
        public const int Health = 1;
        public const int Attack = 2;
        public const int Defense = 3;
        // 命中相关
        public const int Hit = 4;
        public const int Dodge = 5;
        // 暴击相关
        public const int Crit = 6;
        public const int CritDamage = 7;
        // 破甲
        public const int ArmorPierce = 8;
        // 韧性
        public const int Tenacity = 9;
    }

    //属性快照切片
    [Serializable]
    public partial struct StdPropertySnapshot
    {
        //基础属性
        public double Health;
        public double Attack;
        public double Defense;
        //命中相关
        public double Hit;
        public double Dodge;
        //暴击相关
        public double Crit;
        public double CritDamage;
        //破甲
        public double ArmorPierce;
        //韧性
        public double Tenacity;

                

        // 将另一个 PropertySnapshot 的值加到当前实例上
        public void Add(in StdPropertySnapshot other)
        {
            // 基础属性
            Health += other.Health;
            Attack += other.Attack;
            Defense += other.Defense;
            // 命中相关
            Hit += other.Hit;
            Dodge += other.Dodge;
            // 暴击相关
            Crit += other.Crit;
            CritDamage += other.CritDamage;
            // 破甲
            ArmorPierce += other.ArmorPierce;
            //韧性
            Tenacity += other.Tenacity;
        }
        
        public bool AddPropertyByKey(int key, double valueAdd)
        {
            WLogger.Log($"SetPropertyByAttributeKey key={key}, valueAdd={valueAdd}");
            switch (key)
            {
                // 基础属性
                case PropertyFloat.Health:
                    Health += valueAdd;
                    break;
                case PropertyFloat.Attack:
                    Attack += valueAdd;
                    break;
                case PropertyFloat.Defense:
                    Defense += valueAdd;
                    break;
                // 命中相关
                case PropertyFloat.Hit:
                    Hit += valueAdd;
                    break;
                case PropertyFloat.Dodge:
                    Dodge += valueAdd;
                    break;
                // 暴击相关
                case PropertyFloat.Crit:
                    Crit += valueAdd;
                    break;
                case PropertyFloat.CritDamage:
                    CritDamage += valueAdd;
                    break;
                // 破甲
                case PropertyFloat.ArmorPierce:
                    ArmorPierce += valueAdd;
                    break;
                // 韧性
                case PropertyFloat.Tenacity:
                    Tenacity += valueAdd;
                    break;
                default:
                    WLogger.LogError($"SetPropertyByAttributeKey 无效的 AttributeKey={key}");
                    return false;
            }
            return true;
        }
        
        //从个体的动态属性填充： （eg:受短暂Buff修改的属性）
        public bool FillFromEntity(LogicEntity e)
        {
            if (e == null) 
                return false;
            
            //TODO：通过属性组件填充
            Add_FromPlayerCategoryVolume(e);
            return true;
        }

        //叠加集体属性: 玩家身上记录的集体生效的分类属性（ UnitTag组件记录的分类 : UnitTid, Camp, BattleUnitType, Element）
        public void Add_FromPlayerCategoryVolume(LogicEntity e)
        {
            // if (e.GetBattleUnitTag(out var unitTag))
            // {
            //     var playerInfo = e.GetPlayerInfo();
            //     if (playerInfo == null)
            //     {
            //         //G.LogError("叠加 玩家身上记录的集体生效的分类属性 playerInfo == null");
            //         return;
            //     }
            //     
            //     Add(playerInfo.VolumePlayer.Property);
            //     
            //     var volumeInfoBattleUnit = playerInfo.GetVolume_BattleUnit(unitTag.BattleUnitTid ?? 0);
            //     if (volumeInfoBattleUnit != null)
            //     {
            //         Add(volumeInfoBattleUnit.Property);
            //     }
            //
            //     var volumeInfoCamp = playerInfo.GetVolume_Camp(unitTag.Camp ?? 0);
            //     if (volumeInfoCamp != null)
            //     {
            //         Add(volumeInfoCamp.Property);
            //     }
            //     
            //     var volumeInfoUnitType = playerInfo.GetVolume_UnitType(unitTag.BattleUnitType ?? 0);
            //     if (volumeInfoUnitType != null)
            //     {
            //         Add(volumeInfoUnitType.Property);
            //     }
            //     
            //     var volumeInfoElement = playerInfo.GetVolume_Element(unitTag.Element ?? 0);
            //     if (volumeInfoElement != null)
            //     {
            //         Add(volumeInfoElement.Property);
            //     }
            // }
            // else
            // {
            //     WLogger.LogError("叠加 玩家身上记录的集体生效的分类属性 unitTag == null");
            // }
        }
        
        public static StdPropertySnapshot operator +(StdPropertySnapshot a, StdPropertySnapshot b)
        {
            StdPropertySnapshot result = a;
            result.Add(b);
            return result;
        }
    }
}