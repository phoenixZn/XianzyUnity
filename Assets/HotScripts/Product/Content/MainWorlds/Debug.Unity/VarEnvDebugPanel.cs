using System;
using System.Collections.Generic;
using Xease.CoreGame;
using Sirenix.OdinInspector;
using UnityEngine;

/// <summary>
/// Inspector 用 VarEnv 分桶类型勾选过滤：All 主控 + 9 类 bucket，可独立嵌入或作为 VarEnvDebugPanel 子块。
/// </summary>
[Serializable]
public class VarEnvDebugBucketFilterInspector
{
    // 每个都加上则横排
    // [HorizontalGroup("Filter")]
    // [HorizontalGroup("Filter/Row1")]
    [ToggleLeft]
    [LabelText("All")]
    [OnValueChanged(nameof(OnAllChanged))]
    public bool All = true;

    [ToggleLeft]
    [LabelText("int")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Int = true;

    [ToggleLeft]
    [LabelText("float")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Float = true;

    [ToggleLeft]
    [LabelText("long")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Long = true;

    [ToggleLeft]
    [LabelText("bool")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Bool = true;

    [ToggleLeft]
    [LabelText("Vector3")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Vector3 = true;

    [ToggleLeft]
    [LabelText("Vector2")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Vector2 = true;

    [ToggleLeft]
    [LabelText("object")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Object = true;

    [ToggleLeft]
    [LabelText("Enum")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Enum = true;

    [ToggleLeft]
    [LabelText("Other")]
    [OnValueChanged(nameof(OnTypeChanged))]
    public bool Other = true;

    // 防止 All 与单项 OnValueChanged 互相触发
    bool _syncing;

    /// <summary>
    /// 按 VarEnv 分桶键判定是否收集；映射规则与 VarEnvDebugSnapshot.DispatchBucket 分支一致。
    /// </summary>
    /// <param name="bucketType">VarEnv 类型分桶键。</param>
    public bool ShouldIncludeBucket(Type bucketType)
    {
        if (bucketType == typeof(int))
        {
            return Int;
        }

        if (bucketType == typeof(float))
        {
            return Float;
        }

        if (bucketType == typeof(long))
        {
            return Long;
        }

        if (bucketType == typeof(bool))
        {
            return Bool;
        }

        if (bucketType == typeof(Vector3))
        {
            return Vector3;
        }

        if (bucketType == typeof(Vector2))
        {
            return Vector2;
        }

        if (bucketType == typeof(object))
        {
            return Object;
        }

        if (bucketType.IsEnum)
        {
            return Enum;
        }

        return Other;
    }

    void OnAllChanged()
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        SetAllTypeFilters(All);
        _syncing = false;
    }

    void OnTypeChanged()
    {
        if (_syncing)
        {
            return;
        }

        _syncing = true;
        SyncAllFromTypes();
        _syncing = false;
    }

    void SetAllTypeFilters(bool enabled)
    {
        Int = enabled;
        Float = enabled;
        Long = enabled;
        Bool = enabled;
        Vector3 = enabled;
        Vector2 = enabled;
        Object = enabled;
        Enum = enabled;
        Other = enabled;
    }

    void SyncAllFromTypes()
    {
        All = AreAllTypeFiltersEnabled();
    }

    bool AreAllTypeFiltersEnabled()
    {
        return Int
            && Float
            && Long
            && Bool
            && Vector3
            && Vector2
            && Object
            && Enum
            && Other;
    }
}

/// <summary>
/// Inspector 用 VarEnv 黑板快照块：类型勾选过滤 + 只读条目列表。
/// 供 LogicEntityDebugger 各 partial 段通过 [InlineProperty] 嵌入复用。
/// </summary>
/// <remarks>
/// 嵌入步骤：
/// 1. 在 Ex.Xxx.cs 的 [BoxGroup] 内声明 [InlineProperty, HideLabel] VarEnvDebugPanel XxxVarEnvPanel
/// 2. UpdateComData_ComXxx 中 XxxVarEnvPanel.Refresh(varEnvRef, debugger, XxxVarEnvPanel.Filter.ShouldIncludeBucket)
/// 3. 组件不可用分支调用 XxxVarEnvPanel.ClearDisplay()，不重置 Filter 勾选
/// </remarks>
[Serializable]
public class VarEnvDebugPanel
{
    [InlineProperty]
    [HideLabel]
    public VarEnvDebugBucketFilterInspector Filter = new();

    [ReadOnly]
    [ListDrawerSettings(ShowIndexLabels = false, NumberOfItemsPerPage = 30)]
    public List<VarDisplayInfo> DisplayList = new();

    /// <summary>
    /// 从 env 收集黑板条目写入 DisplayList；Filter 勾选状态保留。
    /// </summary>
    /// <param name="env">目标黑板；null 时 Collect 不写条目。</param>
    /// <param name="vfriend">IVarEnvFriend 友元凭证。</param>
    /// <param name="shouldIncludeBucket">分桶过滤，由调用方注入。</param>
    public void Refresh(VarEnv env, IVarEnvFriend vfriend, Func<Type, bool> shouldIncludeBucket)
    {
        VarEnvDebugSnapshot.CollectVarDisplayInfoList(env, vfriend, DisplayList, shouldIncludeBucket);
    }

    /// <summary>
    /// 仅清空展示列表，不清 Filter 勾选（跨实体保留用户偏好）。
    /// </summary>
    public void ClearDisplay()
    {
        DisplayList.Clear();
    }
}
