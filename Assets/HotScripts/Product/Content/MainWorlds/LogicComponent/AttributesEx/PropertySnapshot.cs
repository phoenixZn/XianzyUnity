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
        public const int Hit = 10;
        public const int Dodge = 11;
        
        // 暴击相关
        public const int Crit = 20;
        public const int CritDamage = 22;
        
        // 破甲
        public const int ArmorPierce = 40;
        
        // 韧性
        public const int Tenacity = 60;             
        
        //中间计算值-基础静态配置上的缩放:
        public const int ScaleHpBase = 102;
        public const int ScaleAtkBase = 103;
        //最终伤害修正
        public const int ScaleFinalDamage = 110;
    }

    //属性快照切片
    [Serializable]
    public partial struct PropertySnapshot
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

        //中间计算值:
        public double ScaleHpBase;
        public double ScaleAtkBase;
        public double ScaleFinalDamage;

        
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
                
                // 中间计算值:
                case PropertyFloat.ScaleAtkBase:
                    ScaleAtkBase += valueAdd;
                    break;
                case PropertyFloat.ScaleHpBase:
                    ScaleHpBase += valueAdd;
                    break;
                case PropertyFloat.ScaleFinalDamage:
                    ScaleFinalDamage += valueAdd;
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
            if (!e.hasComAttributes)
                return false;

            var comAttributes = e.comAttributes;
            comAttributes.TryGetValue<double>(PropertyFloat.Health, out Health, 300);
            comAttributes.TryGetValue<double>(PropertyFloat.Attack, out Attack, 100);
            comAttributes.TryGetValue<double>(PropertyFloat.Defense, out Defense, 100);
            comAttributes.TryGetValue<double>(PropertyFloat.Hit, out Hit, 10000f);
            comAttributes.TryGetValue<double>(PropertyFloat.Dodge, out Dodge, 0f);
        
            comAttributes.TryGetValue<double>(PropertyFloat.Crit, out Crit, 0f);
            comAttributes.TryGetValue<double>(PropertyFloat.CritDamage, out CritDamage, 0f); // 10000f == +%100
            
            comAttributes.TryGetValue<double>(PropertyFloat.ArmorPierce, out ArmorPierce, 0);  // 0 - 10000f
            
            comAttributes.TryGetValue<double>(PropertyFloat.Tenacity, out Tenacity, 0f);
            
            comAttributes.TryGetValue<double>(PropertyFloat.ScaleHpBase, out ScaleHpBase, 0f);
            comAttributes.TryGetValue<double>(PropertyFloat.ScaleAtkBase, out ScaleAtkBase, 0f);
            comAttributes.TryGetValue<double>(PropertyFloat.ScaleFinalDamage, out ScaleFinalDamage, 0f);
            
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
            //         //KLogger.LogError("叠加 玩家身上记录的集体生效的分类属性 playerInfo == null");
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

        

        // 将另一个 PropertySnapshot 的值加到当前实例上
        public void Add(in PropertySnapshot other)
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

            ScaleHpBase += other.ScaleHpBase;
            ScaleAtkBase += other.ScaleAtkBase;
            ScaleFinalDamage += other.ScaleFinalDamage;
        }

        public static PropertySnapshot operator +(PropertySnapshot a, PropertySnapshot b)
        {
            PropertySnapshot result = a;
            result.Add(b);
            return result;
        }
    }
}