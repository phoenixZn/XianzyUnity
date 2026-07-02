// // LogicEntityDebuggerEx.MainFSM — MainFSMComponent 段的 Inspector 外显。
// //
// // 【职责边界】主 FSM 相关调试展示只改本文件：
// //   1. [BoxGroup("[MainFSMComponent]")] + [SerializeField] 字段
// //   2. UpdateComData_ComFSM() — 从 comFSM.Logic 只读同步状态与 VarEnv 黑板快照
// //
// // 【与 AI 段差异】AI 手动指定 CvKey；本段通过 IVarEnvFriend 友元 API 自动枚举全部可读黑板变量。
// //
// using Xease.CoreGame;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// public partial class LogicEntityDebugger
// {
//     [BoxGroup("[MainFSMComponent]")]
//     [ReadOnly]
//     [SerializeField] string MainFsmCurrentStateID;
//
//     [BoxGroup("[MainFSMComponent]")]
//     [ReadOnly]
//     [SerializeField] int MainFsmLogicConfigID;
//
//     [BoxGroup("[MainFSMComponent]")]
//     [SerializeField] Vector3 CV_TargetPos;
//
//     [BoxGroup("[MainFSMComponent]")]
//     [InlineProperty]
//     [HideLabel]
//     [SerializeField] VarEnvDebugPanel MainFsmVarEnvPanel = new();
//
//     void UpdateComData_ComFSM()
//     {
//         if (!IsLinkedEntityValid() || !_entity.hasComFSM || _entity.comFSM.Logic == null)
//         {
//             ClearMainFsmDisplay();
//             return;
//         }
//
//         var logic = _entity.comFSM.Logic;
//         MainFsmCurrentStateID = logic.MainFsmNode?.CurrentStateID;
//         MainFsmLogicConfigID = logic.GenInfo?.LogicConfigID ?? 0;
//
//         if (logic.VarEnvRef.ReadVar(CvKey.CV_TargetPos, out Vector3 targetPos))
//         {
//             CV_TargetPos = targetPos;
//         }
//
//         // this 作 IVarEnvFriend 凭证，按类型桶白名单遍历黑板
//         MainFsmVarEnvPanel.Refresh(
//             logic.VarEnvRef, this, MainFsmVarEnvPanel.Filter.ShouldIncludeBucket);
//     }
//
//     // 实体无 comFSM 或 Logic 为空时，重置 Inspector 展示
//     void ClearMainFsmDisplay()
//     {
//         MainFsmCurrentStateID = null;
//         MainFsmLogicConfigID = 0;
//         CV_TargetPos = Vector3.zero;
//         MainFsmVarEnvPanel.ClearDisplay();
//     }
// }
