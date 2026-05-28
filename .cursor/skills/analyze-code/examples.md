# 代码分析技能：示例

以下示例说明如何填写参数以及报告应覆盖的要点（真实项目路径请替换）。

## 示例 A：单文件

**输入（用户消息）**

```markdown
## 本次分析参数
- target: Assets/Script/HotUpdate/Product/EnvStates/EnvStates/EnvBattleState.cs
- focus: 状态进入与退出时订阅了哪些事件
- output_path: docs/analysis/EnvBattleState.md
```

**Agent 行为要点**

- 通读该文件及直接 `#include`/using 的关键依赖。
- 功能清单中每个功能点尽量对应到具体方法名。
- 设计思路中写清与 `EnvMainState` 等相邻状态的关系（若代码中有引用）。

**报告片段期望（结构示意）**

- 概述中写明文件路径与「战斗场景环境状态」类职责。
- 功能清单含 `OnEnter`/`OnExit` 或等价生命周期方法及事件订阅列表。

## 示例 B：模块文件夹

**输入**

```markdown
## 本次分析参数
- target: Assets/Script/HotUpdate/Product/Modules/ModPet
- code_root: Assets/Script/HotUpdate/Product
- context: 宠物系统在战斗外与背包有联动
- output_path: docs/analysis/ModPet-module.md
```

**Agent 行为要点**

- 列出目录内主要 `.cs` 文件，标出对外入口类（如 `ModPet`）。
- 功能按「对外能力」聚合，避免逐文件流水账。
- 依赖关系区分模块内与对 `CoreGame`、其他 `Modules` 的引用。

## 示例 C：仅类型名 + 搜索范围

**输入**

```markdown
## 本次分析参数
- target: GameUtility
- search_root: Assets/Script/HotUpdate/Product/Util
- focus: 与战斗无关的通用工具范围
```

**Agent 行为要点**

- 在 `search_root` 下定位 `class GameUtility`（或同名类型）所在文件。
- 若多个匹配，在概述中列出候选并说明选择了哪一个及原因。
