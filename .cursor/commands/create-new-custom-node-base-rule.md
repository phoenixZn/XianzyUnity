# create-new-custom-node-base-rule

## meta

```
meta.type: command_spec
meta.domain: CustomLogic
meta.language: zh-CN
meta.scope: 新增「可注册、可被工厂创建」的节点（运行时类 + 配置类 + NodeConfigTypeRegistry 注册）
meta.consumers: create-new-bhv-node.md、create-new-state-node.md 等
meta.consumer_obligation: 生成代码 MUST 与本 spec 一致；分类型文件仅补充 kind 专用基类、参数与自检项
```

## success_criteria

- `CustomLogicFactory.CreateCustomNode` 与 XML `CLHelper.CreateNodeCfg` 能按 `type` 创建节点
- 单文件内完成：`Register` + `XxxCfg` + `Xxx`（对齐 `LogBhv.cs` 模式）

## deliverables_default

```
deliverable.primary: 单个 .cs 文件
deliverable.in_file.1: public static partial class NodeConfigTypeRegistry 内 Register(typeof(XxxCfg), NodeCategory.*)
deliverable.in_file.2: XxxCfg 实现 ICustomNodeCfg；若走 XML 则实现 IParseFromXml
deliverable.in_file.3: 运行时类 Xxx
deliverable.secondary: 若用户要求 XML 片段或改 LogicConfig*.cs → 第二段输出或第二个补丁；对话中声明
```

## naming_and_registration (HARD)

```
XML: attribute type="Foo"  =>  config class name MUST be FooCfg  (CLHelper: type + "Cfg")
Register line pattern:
  static bool _XxxCfg = Register(typeof(XxxCfg), NodeCategory.Bhv | NodeCategory.Cnd | NodeCategory.State | ...);
NodeCategory: 与语义一致；枚举定义见 NodeConfigTypeRegistry.cs
MUST NOT: 新建非 partial 的 NodeConfigTypeRegistry；仅扩展现有 partial
```

## runtime_root

- 子树运行时节点：`CustomNode` / `ICustomNode`（`VarEnv`、`InitializeNode`、`Activate`/`Deactivate`、`IsActive`）
- entry: `Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/CustomNode/CustomNode.cs`

## repo_paths (read_before_implement)

```
custom_node_base: Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/CustomNode/CustomNode.cs
inner_runtime: Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/Interface/Inner/IInnerRuntime.cs
cl_helper_xml: Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/Util/CLHelper.cs
registry_category: Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/Config/NodeConfigTypeRegistry.cs
custom_logic_root: Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/CustomLogic.cs
behavior_base: Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/BehaviorNodes/Base/BehaviorNodeBase.cs
behavior_full_sample: Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/BehaviorNodes/Common/LogBhv.cs
condition_base: Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/ConditionNodes/Base/ConditionNodeBase.cs
condition_short_sample: Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/ConditionNodes/AwaysTrueCnd.cs
state_base: Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/FSMNodes/Base/StateNode.cs
state_bhv_exit_sample: Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/FSMNodes/Common/CustomBhvState.cs
```

## directory_rule

- 通用、与业务无关 → `CustomNodes.Foundation`
- 玩法/业务/技能相关 → `Product/.../CustomNodes.Ex`（状态多继承 `CustomBhvState`）

## framework_behavior (assembly_facts)

- 根 `CustomLogic` 经工厂建子节点
- `NodeConfigTypeRegistry`：`XML type` → `*Cfg`
- `Update`：仅对实现 `INeedUpdate` 且 `IsActive` 的节点
- `CanStop`：子树聚合 `INeedStopCheck`

## output_style

- `namespace HotUpdate.CoreGame`（除非目标目录已有约定且用户指定）
- 错误日志优先 `CLHelper.LogError(this, "...")`
- 仅输出请求文件；禁止无关重构；禁止删除他人 `Register` 条目

## MUST_follow

1. 覆写 `Destroy()` / `Reset()` 时若基类可链式，MUST 调用 `base.Destroy()` / `base.Reset()`
2. 子节点由 `CustomLogicFactory` 与父销毁回收；业务节点 MUST NOT 对已交给工厂的子节点再 `DestroyCustomNode`（除非本地明确持有且文档约定）
3. 「Destroy 后等价刚 new」= 本类字段已清空；池化可复用；MUST NOT 假设跨逻辑实例引用仍有效

## code_only_note

- 无 XML：`new XxxCfg { ... }` 挂父配置子列表可行
- MUST 在某 `.cs` 完成 `Register`，否则运行期无法按名创建
- 若调用链从不走 `CreateNodeCfg` 字符串、仅 `new` + `NodeType()` 直建：须自行核对是否绕过注册表；本仓库常规路径均需注册
