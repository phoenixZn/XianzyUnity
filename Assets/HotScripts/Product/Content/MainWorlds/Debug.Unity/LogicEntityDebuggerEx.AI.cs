// // LogicEntityDebuggerEx.AI — AIComponent 段的 Inspector 外显与 Scene Gizmos。
// //
// // 【职责边界】AI 相关调试展示只改本文件：
// //   1. [BoxGroup("[AIComponent]")] + [SerializeField] 字段
// //   2. UpdateComData_ComAI() — 从 comAI.Logic.VarEnvRef 只读同步
// //   3. DrawGizmos_ComAI() — 行军目标等 Scene 可视化
// //
// // 【Agent 扩展 AI 段步骤】
// //   1. 在本文件增加 [SerializeField]（类型与 CvKey / 黑板一致）。
// //   2. 在 UpdateComData_ComAI() 中 ReadVar 写入字段。
// //   3. 若需 Gizmos：扩展 DrawGizmos_ComAI()，并在 LogicEntityDebuggerEx.cs 的 DrawDebugGizmos() 中确认已调用。
// //
// using Xease.CoreGame;
// using Sirenix.OdinInspector;
// using UnityEngine;
//
// public partial class LogicEntityDebugger
// {
//     [BoxGroup("[AIComponent]")]
//     [SerializeField] float CV_SearchRange;
//     [BoxGroup("[AIComponent]")]
//     [SerializeField] Vector3 CV_FixedMoveDir;
//     [BoxGroup("[AIComponent]")]
//     [SerializeField] Vector3 CV_MarchTargetPos;
//
//     // [Header("AIComponent")]
//     void UpdateComData_ComAI()
//     {
//         if (!_entity.hasComAI || _entity.comAI.Logic == null)
//         {
//             return;
//         }
//
//         var varEnv = _entity.comAI.Logic.VarEnvRef;
//         if (varEnv.ReadVar(CvKey.CV_SearchRange, out float searchRange))
//         {
//             CV_SearchRange = searchRange;
//         }
//
//         if (varEnv.ReadVar(CvKey.CV_FixedMoveDir, out Vector3 fixedMoveDir))
//         {
//             CV_FixedMoveDir = fixedMoveDir;
//         }
//
//         if (varEnv.ReadVar(CvKey.CV_MarchTargetPos, out Vector3 marchTargetPos))
//         {
//             CV_MarchTargetPos = marchTargetPos;
//         }
//     }
//
//     void DrawGizmos_ComAI()
//     {
//         if (CV_MarchTargetPos == Vector3.zero)
//         {
//             return;
//         }
//         Gizmos.color = Color.red;
//         Gizmos.DrawLine(ComPos, CV_MarchTargetPos);
//         Gizmos.DrawSphere(CV_MarchTargetPos, 0.25f);
//     }
// }
