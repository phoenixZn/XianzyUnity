using System;
using System.Collections;
using System.Collections.Generic;
using Xease.CoreGame;
using UnityEngine;

/// <summary>
/// Inspector 单行黑板条目：Key + 类型简名 + 格式化后的可读值。
/// </summary>
[Serializable]
public struct VarDisplayInfo
{
    public string Key;
    public string TypeName;
    public string Value;
}

/// <summary>
/// VarEnv 黑板调试快照：按类型桶白名单遍历，仅保留便于阅读的条目。
/// </summary>
public static class VarEnvDebugSnapshot // 借助 IVarEnvFriend 友元 API 只读枚举 VarEnv 黑板，供 LogicEntityDebugger 展示。
{
    /// <summary>
    /// 收集 env 中可格式化的黑板变量，结果写入 displayList（会先 Clear）。
    /// </summary>
    /// <param name="env">目标黑板；null 时不写入。</param>
    /// <param name="vfriend">IVarEnvFriend 友元凭证；null 时 VarEnv 拒绝遍历。</param>
    /// <param name="displayList">输出列表，调用前会被 Clear。</param>
    /// <param name="shouldIncludeBucket">分桶过滤；null 时不过滤、收集全部 bucket。</param>
    public static void CollectVarDisplayInfoList(
        VarEnv env,
        IVarEnvFriend vfriend,
        List<VarDisplayInfo> displayList,
        Func<Type, bool> shouldIncludeBucket = null)
    {
        displayList.Clear();
        if (env == null || vfriend == null)
        {
            return;
        }

        env.ForeachBuckets(vfriend, (bucketType, variables) =>
        {
            DispatchBucket(bucketType, variables, vfriend, displayList, shouldIncludeBucket);
        });

        displayList.Sort(CompareDisplayByKey);
    }

    // 按 VarEnv 分桶键 dispatch；未列入白名单的 bucketType 直接跳过
    static void DispatchBucket(
        Type bucketType,
        IVariables variables,
        IVarEnvFriend vfriend,
        List<VarDisplayInfo> displayList,
        Func<Type, bool> shouldIncludeBucket)
    {
        if (variables == null)
        {
            return;
        }

        if (shouldIncludeBucket != null && !shouldIncludeBucket(bucketType))
        {
            return;
        }

        if (bucketType == typeof(int))
        {
            CollectBucket((VariablesImp<int>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(float))
        {
            CollectBucket((VariablesImp<float>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(long))
        {
            CollectBucket((VariablesImp<long>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(bool))
        {
            CollectBucket((VariablesImp<bool>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(string))
        {
            CollectBucket((VariablesImp<string>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(Vector3))
        {
            CollectBucket((VariablesImp<Vector3>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(Vector2))
        {
            CollectBucket((VariablesImp<Vector2>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType == typeof(object))
        {
            CollectBucket((VariablesImp<object>)variables, vfriend, bucketType, displayList);
        }
        else if (bucketType.IsEnum)
        {
            // enum 使用 ForeachCollect
            BucketForeachCollect(variables, vfriend, bucketType, displayList);
        }
        else
        {
            // 其它未列举值类型，也使用 ForeachCollect，回头可能会打Log发现
            BucketForeachCollect(variables, vfriend, bucketType, displayList);
        }
    }

    static void CollectBucket<T>(VariablesImp<T> imp, IVarEnvFriend vfriend, Type bucketType, List<VarDisplayInfo> displayList)
    {
        var dict = imp.GetRawDictionary(vfriend);
        if (dict == null)
        {
            return;
        }

        foreach (var kv in dict)
        {
            if (typeof(T) == typeof(object))
            {
                if (!TryFormatObjectBucketValue(kv.Value, out string typeName, out string valueText))
                {
                    continue;
                }

                TryAddDisplay(displayList, kv.Key, typeName, valueText);
                continue;
            }

            TryAddDisplay(displayList, kv.Key, bucketType.Name, FormatTypedValue(kv.Value));
        }
    }

    static void BucketForeachCollect(IVariables variables, IVarEnvFriend vfriend, Type bucketType, List<VarDisplayInfo> displayList)
    {
        variables.ForeachCollect((key, value) =>
        {
            TryAddDisplay(displayList, key, bucketType.Name, FormatTypedValue(value));
        });
    }

    static string FormatTypedValue<T>(T value)
    {
        if (value == null)
        {
            return "null";
        }
        return value.ToString();
    }

    static void TryAddDisplay(List<VarDisplayInfo> displayList, string key, string typeName, string valueText)
    {
        displayList.Add(new VarDisplayInfo
        {
            Key = key,
            TypeName = typeName,
            Value = valueText,
        });
    }

    static int CompareDisplayByKey(VarDisplayInfo a, VarDisplayInfo b)
    {
        int typeCompare = string.CompareOrdinal(a.TypeName, b.TypeName);
        if (typeCompare != 0)
        {
            return typeCompare;
        }

        return string.CompareOrdinal(a.Key, b.Key);
    }

    // object 桶：string / LogicEntity / 标准容器数量 / 装箱值类型；其余复杂引用过滤
    static bool TryFormatObjectBucketValue(object value, out string typeName, out string valueText)
    {
        typeName = null;
        valueText = null;

        if (value == null)
        {
            typeName = "null";
            valueText = "null";
            return true;
        }

        var runtimeType = value.GetType();
        typeName = runtimeType.Name;

        if (value is string || value is LogicEntity)
        {
            valueText = FormatReferenceValue(value);
            return valueText != null;
        }

        if (TryFormatContainerCount(value, runtimeType, out valueText))
        {
            return true;
        }

        if (ShouldSkipReferenceType(runtimeType))
        {
            return false;
        }

        // object 桶内装箱的值类型 struct
        if (runtimeType.IsValueType)
        {
            valueText = value.ToString();
            return true;
        }

        return false;
    }

    // List / Dictionary / 原生数组等容器：仅展示元素数量
    static bool TryFormatContainerCount(object value, Type runtimeType, out string valueText)
    {
        valueText = null;

        if (runtimeType.IsArray)
        {
            valueText = $"Length={((Array)value).Length}";
            return true;
        }

        if (value is ICollection dictionary)
        {
            valueText = $"Count={dictionary.Count}";
            return true;
        }
        
        return false;
    }

    // Proto 配置、世界、玩家信息等复杂引用不在 Inspector 展示
    static bool ShouldSkipReferenceType(Type runtimeType)
    {
        // if (typeof(IMessage).IsAssignableFrom(runtimeType)
        //     || typeof(LogicWorld).IsAssignableFrom(runtimeType)
        //     || typeof(MetaWorld).IsAssignableFrom(runtimeType)
        //     || typeof(IBattlePlayerInfo).IsAssignableFrom(runtimeType))
        // {
        //     return true;
        // }
        return false;
        //return runtimeType.IsClass || runtimeType.IsInterface;
    }

    static string FormatReferenceValue(object value)
    {
        if (value is LogicEntity entity)
        {
            return $"LogicEntity(ID={entity.ID})";
        }

        return value.ToString();
    }
}
