# 分析参数模板（复制后填写）

将下方内容复制到对话中（或保存为个人本地文件，勿提交敏感路径）。

```markdown
## 本次分析参数
- target:
- target_file:
- search_root:
- code_root:
- context:
- focus:
- output_path:
```

## 字段说明

| 字段 | 示例 | 备注 |
|------|------|------|
| target | `Assets/Script/Foo/Bar.cs` | 文件、文件夹或类型名 |
| target_file | `Assets/Script/Foo/Bar.cs` | 仅类名分析时建议填写 |
| search_root | `Assets/Script/HotUpdate/Product/Modules` | 缩小按类名搜索范围 |
| code_root | 仓库根或某顶层目录 | 未填默认工作区根 |
| context | 本模块属于战斗子系统 | 可选背景 |
| focus | 重点关注生命周期与状态切换 | 可选 |
| output_path | `docs/analysis/bar.md` | 可选；未填仅对话输出 |

## 典型组合

**分析单个文件**

- `target` = 文件路径，其余可空。

**分析模块目录**

- `target` = 文件夹路径；`code_root` 可空（以 `target` 为边界）。

**仅知道类名**

- `target` = `MyBattleState`
- `target_file` = 定义文件路径（推荐），或提供 `search_root` 让 Agent 搜索定位。
