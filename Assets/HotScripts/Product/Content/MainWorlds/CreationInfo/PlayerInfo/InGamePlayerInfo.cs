using System.Collections.Generic;

namespace Xease.CoreGame
{
    public partial class InGamePlayerInfo
    {
        public PlayerFeaturesContext FeaturesContext = new ();
        public CategoryStatsVolume StatsVolumePlayer { get; private set; }
        private readonly Dictionary<ECategoryVolume, object> _categoryVolumes = new();
        
        public void InitPlayerStats()
        {
            //玩家全局特性：
            StatsVolumePlayer = new();
            
            //玩家细分特性：所有分类字典
            _categoryVolumes[ECategoryVolume.BattleUnit] = new Dictionary<int, CategoryStatsVolume>();
            _categoryVolumes[ECategoryVolume.Skill] = new Dictionary<int, CategoryStatsVolume>();
            _categoryVolumes[ECategoryVolume.SubobjectTid] = new Dictionary<int, CategoryStatsVolume>();
            
            // _categoryVolumes[ECategoryVolume.UnitType] = new Dictionary<TBattleUnitType, CategoryVolumeInfo>();
            // _categoryVolumes[ECategoryVolume.Camp] = new Dictionary<TCampType, CategoryVolumeInfo>();
            // _categoryVolumes[ECategoryVolume.Element] = new Dictionary<TElementType, CategoryVolumeInfo>();
            // _categoryVolumes[ECategoryVolume.LevelType] = new Dictionary<TLevelType, CategoryVolumeInfo>();
        }
        
        protected TVolume GetCategory<TKey, TVolume>(ECategoryVolume volumeType, TKey key, bool autoCreate = false) 
            where TKey : notnull 
            where TVolume : CategoryStatsVolume, new()
        {
            if (_categoryVolumes.TryGetValue(volumeType, out var dictObj) && dictObj is Dictionary<TKey, CategoryStatsVolume> dict)
            {
                if (dict.TryGetValue(key, out var category))
                    return category as TVolume;
                
                if (autoCreate)
                {
                    category = new TVolume();
                    dict[key] = category;
                    return category as TVolume;
                }
            }
            return null;
        }

        // 特定类型的便捷方法
        public CategoryStatsVolume GetVolume_BattleUnit(int battleUnitId, bool autoCreate = false) 
            => GetCategory<int, CategoryStatsVolume>(ECategoryVolume.BattleUnit, battleUnitId, autoCreate);
        public CategoryStatsVolume GetVolume_Skill(int skillTid, bool autoCreate = false) 
            => GetCategory<int, CategoryStatsVolume>(ECategoryVolume.Skill, skillTid, autoCreate);
        public CategoryStatsVolumeSubobject GetVolume_SubobjectTid(int tid, bool autoCreate = false) 
            => GetCategory<int, CategoryStatsVolumeSubobject>(ECategoryVolume.SubobjectTid, tid, autoCreate);

        // public CategoryVolumeInfo GetVolume_UnitType(TBattleUnitType unitType, bool autoCreate = false) 
        //     => GetCategory<TBattleUnitType, CategoryVolumeInfo>(ECategoryVolume.UnitType, unitType, autoCreate);
        // public CategoryVolumeInfo GetVolume_Camp(TCampType camp, bool autoCreate = false) 
        //     => GetCategory<TCampType, CategoryVolumeInfo>(ECategoryVolume.Camp, camp, autoCreate);
        // public CategoryVolumeInfo GetVolume_Element(TElementType element, bool autoCreate = false) 
        //     => GetCategory<TElementType, CategoryVolumeInfo>(ECategoryVolume.Element, element, autoCreate);
        // public CategoryVolumeInfo GetVolume_LevelType(TLevelType levelType, bool autoCreate = false) 
        //     => GetCategory<TLevelType, CategoryVolumeInfo>(ECategoryVolume.LevelType, levelType, autoCreate);
        
        //继续增补
    }
}