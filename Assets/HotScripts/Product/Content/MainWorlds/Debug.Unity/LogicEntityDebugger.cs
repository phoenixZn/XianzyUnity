// LogicEntityDebugger（主文件）— 运行时调试 MonoBehaviour，partial 与 LogicEntityDebuggerEx.cs 同属一类。
//
// 【职责边界】本文件只放：
//   - Link / Unlink / Detach 生命周期（与 Entitas Retain、对象池入池联动）
//   - MonoBehaviour 入口（Start / Update / OnDrawGizmos 委托）
//   - 最简展示范例：基础信息（EntityID）
// 组件数据的 Inspector 外显、Gizmos 绘制 → 写在 LogicEntityDebuggerEx.cs 及其分文件（如 LogicEntityDebuggerEx.AI.cs）。
//
// 【使用方式】
//   - 调试时可挂到模型 Prefab；Play 下 Start 经 ComUnityObjectRelated 自动 Link。
//   - 回池前Entity KGameObjectView.DisposeView 会 DetachFromGameObject。
//   - 入池前须 DestroyImmediate 移除本组件，禁止仅用 Destroy（会延迟到帧末随实例进池）。
//   - 不要移除 KGameObjectView.DisposeView 中对 DetachFromGameObject 的调用。
//
// 【Agent 扩展约束 — 勿改本文件除非涉及生命周期】
//   - 新增组件展示：不要在本文件加 [SerializeField] / UpdateComData_ComXXX，改 Ex 分文件。
//
// 【Agent 在本文件允许的小改动】
//   - 基础信息段新增 Entity 级字段时，可仿 RefreshBasicInfo + [Header("基础信息")] 模式。
//
using System;
using Entitas;
using Xease;
using Xease.CoreGame;
using UnityEngine;

public partial class LogicEntityDebugger : MonoBehaviour, IVarEnvFriend
{
    LogicEntity _entity;
    bool _applicationIsQuitting;

    public LogicEntity Entity { get { return _entity; } }

    [Header("基础信息")]
    [SerializeField] int EntityID;
    [SerializeField] int GoInstanceID;
    
    //////////////////////////////////////////////////////////////////////////
    // MonoBehaviour:

    void Start()
    {
        GoInstanceID = gameObject.GetInstanceID();
        if (_entity != null)
        {
            return;
        }
        LogicWorld logicWorld = GetLogicWorld();
        if (logicWorld == null)
        {
            G.LogError("LogicEntityDebugger logicWorld == null");
            return;
        }
        TryAutoLinkFromGameObject(logicWorld);
    }

    protected virtual LogicWorld GetLogicWorld()
    {
        var modMainWorld = G.Module<ModuleMainWorld>();
        if (modMainWorld?.MainWorld == null)
        {
            return null;
        }
        return modMainWorld?.MainWorld.LogicWorld;
    }

    void Update()
    {
        if (!IsLinkedEntityValid())
        {
            return;
        }
        RefreshBasicInfo();
        UpdateComData();
    }

    void OnApplicationQuit()
    {
        _applicationIsQuitting = true;
    }

    void OnDestroy()
    {
        if (!_applicationIsQuitting && _entity != null)
        {
            Debug.LogWarning("LogicEntityDebugger got destroyed but is still linked to " + _entity + "!\n" +
                             "Please call Unlink() or DetachFromGameObject() before it is destroyed."
            );
            ReleaseEntityLink();
        }
    }

    void OnDrawGizmos()
    {
        if (!IsLinkedEntityValid())
        {
            return;
        }
        DrawDebugGizmos();
    }

    //////////////////////////////////////////////////////////////////////////
    // Link / Detach:
    public void Link(LogicEntity entity)
    {
        if (_entity != null)
        {
            throw new Exception($"LogicEntityDebugger is already linked to {_entity}");
        }
        _entity = entity;
        _entity.OnDestroyEntity += OnLinkedEntityDestroy;
        _entity.Retain(this);
    }

    public void Unlink()
    {
        if (_entity == null)
        {
            throw new Exception("LogicEntityDebugger is already unlinked!");
        }
        ReleaseEntityLink();
    }
    
    // 解除 Retain 并取消 OnDestroyEntity 订阅；可重复调用
    void ReleaseEntityLink()
    {
        if (_entity == null)
        {
            return;
        }
        _entity.OnDestroyEntity -= OnLinkedEntityDestroy;
        _entity.Release(this);
        _entity = null;
    }
    
    void OnLinkedEntityDestroy(IEntity entity)
    {
        DetachAndDestroyComponent();    //保底机制
    }    

    /////////////////////////// For Pool ////////////////////////////////////////
    /// <summary>
    /// View 回对象池前调用：解除 Entity 引用并同步移除组件，避免 Debugger 随池化实例残留。
    /// </summary>
    public static void DetachFromGameObject(GameObject gameObject)
    {
        if (gameObject == null)
        {
            return;
        }
        var debugger = gameObject.GetComponent<LogicEntityDebugger>();
        if (debugger == null)
        {
            return;
        }
        debugger.DetachAndDestroyComponent();
    }

    // 入池/Entity 销毁时须同步移除，Destroy 延迟到帧末会导致组件仍随实例进池
    void DetachAndDestroyComponent()
    {
        ReleaseEntityLink();
        DestroyImmediate(this);
    }
    
    
    // 按挂载 GameObject 的 InstanceID 查找 UnityObjectRelated 并关联 LogicEntity
    void TryAutoLinkFromGameObject(LogicWorld logicWorld)
    {
        if (_entity != null || logicWorld == null)
        {
            return;
        }

        var entity = logicWorld.GetEntityWithUnityObjectRelated(gameObject.GetInstanceID());
        if (entity == null)
        {
            KLogger.LogError($"LogicEntityDebugger 未找到关联 LogicEntity: {gameObject.name}");
            return;
        }

        Link(entity);
    }

    //////////////////////////////////////////////////////////////////////////
    // Display: 基础信息
    void RefreshBasicInfo()
    {
        if (_entity.hasComID)
        {
            EntityID = (int)_entity.ID;
        }
    }

    bool IsLinkedEntityValid()
    {
        return _entity != null && _entity.isEnabled;
    }
}
