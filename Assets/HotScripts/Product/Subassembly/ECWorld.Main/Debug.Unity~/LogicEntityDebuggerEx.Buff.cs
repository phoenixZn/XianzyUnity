// LogicEntityDebuggerEx.Buff — BuffCenterComponent 段的 Inspector 外显与调试操作。
//
// 【职责边界】Buff 相关调试展示只改本文件：
//   1. [BoxGroup("[BuffCenterComponent]")] + [SerializeField] 字段；按钮同组，仅按钮需 [PropertyOrder(1)]
//   2. UpdateComData_ComBuffCenter() — 从 comBuffCenter.BuffList 只读同步 BuffID
//   3. DebugRemoveAllBuffs() — Inspector 按钮（Stretch=false），调用 RemoveAllBuffs()
//
using System.Collections.Generic;
using HotUpdate.CoreGame;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class LogicEntityDebugger
{
    [BoxGroup("[BuffCenterComponent]")]
    [ReadOnly]
    [SerializeField] List<int> BuffList = new List<int>();

    // 中间缓冲，避免每帧 new List
    readonly List<int> _buffIdScratch = new List<int>();
    
    // 上一帧 Buff 数量，配合逐项比较做增量同步
    int _buffIdsCacheCount = -1;

    [BoxGroup("[BuffCenterComponent]")]
    //[PropertyOrder(1)]
    [Button("移除所有Buff", ButtonSizes.Small, Stretch = false)]
    void DebugRemoveAllBuffs()
    {
        if (!IsLinkedEntityValid() || !_entity.hasComBuffCenter)
        {
            return;
        }

        _entity.comBuffCenter.RemoveAllBuffs();
        BuffList.Clear();
        _buffIdsCacheCount = -1;
    }

    // [Header("[BuffCenterComponent]")]
    void UpdateComData_ComBuffCenter()
    {
        if (!IsLinkedEntityValid() || !_entity.hasComBuffCenter)
        {
            if (BuffList.Count > 0)
            {
                BuffList.Clear();
            }

            _buffIdsCacheCount = -1;
            return;
        }

        var buffList = _entity.comBuffCenter.BuffList;
        int count = buffList.Count;

        if (count == _buffIdsCacheCount && IsBuffIdsUnchanged(buffList, count))
        {
            return;
        }

        _buffIdScratch.Clear();
        for (int i = 0; i < count; i++)
        {
            _buffIdScratch.Add(buffList[i].BuffGenInfo.LogicConfigID);
        }

        BuffList.Clear();
        BuffList.AddRange(_buffIdScratch);
        _buffIdsCacheCount = count;
    }

    // 数量已相同时，逐项比较 LogicConfigID 是否变化
    bool IsBuffIdsUnchanged(List<IBuff> buffList, int count)
    {
        if (count != BuffList.Count)
        {
            return false;
        }

        for (int i = 0; i < count; i++)
        {
            if (buffList[i].BuffGenInfo.LogicConfigID != BuffList[i])
            {
                return false;
            }
        }

        return true;
    }
}
