// LogicEntityDebuggerEx（扩展主文件）— partial 聚合入口与尚未拆出的组件段。
//
// 【文件组织】按组件拆分为 LogicEntityDebuggerEx.*.cs（如 LogicEntityDebuggerEx.AI.cs）；
// 本文件保留 UpdateComData / DrawDebugGizmos 聚合，以及暂未拆出的段（如 Transform）。
//
// 【职责边界】新增/修改调试展示时：
//   - 已有分文件的组件 → 改对应 Ex.*.cs
//   - 新组件 → 新建 LogicEntityDebuggerEx.Xxx.cs，并在本文件聚合入口追加调用
//
// 【Agent 新增一块组件展示的标准步骤】
//   1. 新建或打开对应 Ex 分文件，增加 [Header] 与 [SerializeField]。
//   2. 实现 UpdateComData_ComXxx()：
//        - 先判组件是否存在（如 hasComXxx、Logic != null），不存在则 return；
//        - 只读 _entity.comXxx / VarEnvRef.ReadVar，写入 SerializeField；
//        - 复杂分支或非显然业务规则补 // 单行注释。
//   3. 在本文件 UpdateComData() 末尾追加 UpdateComData_ComXxx() 调用。
//   4. 若需 Gizmos：在分文件实现 DrawGizmos_ComXxx()，并在 DrawDebugGizmos() 中调用。
//   5. 不要改 Link / Detach / Retain 逻辑（留在 LogicEntityDebugger.cs）。
//
// 【数据来源参考】
//   - 组件字段：_entity.hasComXxx → _entity.comXxx
//   - AI 黑板：见 LogicEntityDebuggerEx.AI.cs
//   - FSM 黑板：见 LogicEntityDebuggerEx.MainFSM.cs（IVarEnvFriend 友元 API 自动枚举 VarEnvRef）
//
// 【Inspector 排序约定】
//   - 段间：同 partial 内靠 [BoxGroup] + 声明顺序；不配置 BoxGroup order / 字段 PropertyOrder
//   - 带 [Button] 的段：仅给按钮加 [PropertyOrder(1)]，保证同组内跟在字段后面
//
// 【命名约定】
//   - UpdateComData 与 Header 一一对应：UpdateComData_ComTransform ↔ [Header("TransformComponent")]
//   - Gizmos 同理：DrawGizmos_ComAI ↔ AIComponent 段
//
using Xease.CoreGame;
using Sirenix.OdinInspector;
using UnityEngine;

public partial class LogicEntityDebugger
{
    [BoxGroup("[TransformComponent]")]
    [SerializeField] Vector3 ComPos;
    private Vector3 lastPos;

    
    //////////////////////////////////////////////////////////////////////////
    // UpdateData Display:
    //////////////////////////////////////////////////////////////////////////

    void UpdateComData()
    {
        UpdateComData_ComTransform();
        // UpdateComData_ComAI();
        // UpdateComData_ComBuffCenter();
        // UpdateComData_ComFSM();
    }
    
    // [Header("TransformComponent")]
    void UpdateComData_ComTransform()
    {
        if (!_entity.hasComTransform)
        {
            return;
        }

        if (lastPos != ComPos && lastPos != Vector3.zero)
        {
            KLogger.LogWarning($"LogicEntityDebugger 篡改ComTransform lastPos={lastPos}, ComPos={ComPos}");
            _entity.SetPosition(ComPos);
        }
        ComPos = _entity.comTransform.position;
        lastPos = ComPos;
    }

    //////////////////////////////////////////////////////////////////////////
    // Gizmos:
    //////////////////////////////////////////////////////////////////////////
    void DrawDebugGizmos()
    {
        //DrawGizmos_ComAI();
    }
}
