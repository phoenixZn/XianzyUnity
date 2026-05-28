# create-new-custom-node

在 CustomLogic 体系下新增一个**可注册、可被工厂创建**的节点（运行时类 + 静态配置类 + `NodeConfigTypeRegistry` 注册）。生成代码须符合本仓库现有 Foundation / Product 习惯。

---

## 调用前填写（优先于零散描述）

将本节复制到对话中并填写；**以本节为准**。

```markdown
## 新节点参数
- name_en: （类名前缀，如 MyOpenDoor，则配置类为 MyOpenDoorCfg）
- purpose: （一句话用途）
- kind: Behavior | Condition | State
- state_shape: （仅 kind=State）plain | custom_bhv  
  - plain → 继承 `StateNode`，配置 `StateNodeCfg` 或子类  
  - custom_bhv → 继承 `CustomBhvState`，配置 `CustomBhvStateCfg`（含 Bhv / ExitBhv 子行为）
- config_source: XML | code_only  
  - XML → `*Cfg` 实现 `IParseFromXml`  
  - code_only → 仍须 `Register`；可无 `ParseFromXml`，由 `LogicConfig*.cs` 等代码装配
- output_path: （目标 `.cs` 路径，如 `Assets/Script/HotUpdate/Product/CoreGame/CustomNodes.Ex/Foo/MyNode.cs`）
- extra_deliverables: （可选：无 | XML片段 | 修改某 LogicConfig 文件）
```

未填项由 Agent 根据上下文推断；**`output_path` 不明确时必须先问用户**。

---

## 目标与单文件约定

- **目标**：新增节点可被 `CustomLogicFactory.CreateCustomNode` / XML `CLHelper.CreateNodeCfg` 正确创建。
- **默认交付物**：**一个** `.cs` 文件，且在同文件内包含（与 `LogBhv.cs` 同模式）：
  1. `public static partial class NodeConfigTypeRegistry` 中的 **`Register(typeof(XxxCfg), NodeCategory.*)`**
  2. 配置类 **`XxxCfg`**（实现 `ICustomNodeCfg`；若走 XML 则实现 `IParseFromXml`）
  3. 运行时类 **`Xxx`**
- 若 `extra_deliverables` 要求 XML 或修改 `LogicConfig*.cs`，在**第二段输出**或**第二个补丁**中交付，并在对话中说明。

---

## 基类选型

| kind | 推荐基类 | 说明 |
|------|-----------|------|
| Behavior | `BehaviorNodeBase` 或 `BehaviorNode<TCfg>` | `BehaviorNode<TCfg>` 自动持有强类型 `mCfg`；简单节点可像 `LogBhv` 一样继承 `BehaviorNodeBase` 并 `as XxxCfg`。 |
| Condition | `ConditionBaseCfg` + `ConditionNodeBase` | 实现 `Inner_ConditionCheck()`；组合条件见 `CndListCfg` 等现有基类。 |
| State | `StateNode` 或 `CustomBhvState` | **仅 FSM 状态壳、无嵌入行为树**可用 `StateNode`；**状态内要跑 Bhv/ExitBhv** 用 `CustomBhvState`（项目玩法状态多在 Product 继承此类）。 |

---

## 分类型自检清单（Agent 逐项核对）

### Behavior

- [ ] 需要每帧逻辑：已由 `BehaviorNodeBase` 实现 `INeedUpdate`（`OnBegin` / `OnUpdate`）。
- [ ] **可重复执行**：覆写 `Reset()`，与 `Destroy()` 中对字段的清理语义一致；基类 `Reset()` 会重置 `mHasUpdate`。
- [ ] **未结束则不可销毁**：额外实现 `INeedStopCheck`，与根 `CustomLogic.CanStop()` 聚合一致。
- [ ] 覆写 `Destroy()`：清空本类新增引用/状态，**最后 `base.Destroy()`**。

### Condition

- [ ] `XxxCfg` 继承 `ConditionBaseCfg`（或项目约定的条件配置基类），`NodeType()` 返回运行时类型。
- [ ] `ParseFromXml` 中调用 `base.ParseFromXml` 以处理 `UseUnaryNOT` / `UseFixedResult` 等（见 `ConditionBaseCfg`）。
- [ ] 运行时继承 `ConditionNodeBase`，实现 `Inner_ConditionCheck()`。
- [ ] `Destroy()` 重置条件相关字段并 `base.Destroy()`。

### State

- [ ] 配置侧：`StateNodeCfg` / `CustomBhvStateCfg`，`StateID`、可选 `NextStateID`、`Transitions` 与父 FSM 约定一致。
- [ ] `CustomBhvState` 子类：勿重复销毁由 `Factory` 创建的子节点；遵循基类生命周期。
- [ ] `Destroy()`：`base.Destroy()`。

---

## 命名与注册硬性规则

1. **XML**：节点元素属性 `type="Foo"` → 配置类名必须是 **`FooCfg`**（`CLHelper` 使用 `type + "Cfg"` 查找类型）。
2. **注册**：在同一文件（或既有 partial 文件）中增加：
   `static bool _XxxCfg = Register(typeof(XxxCfg), NodeCategory.Bhv | Cnd | State | …);`
   `NodeCategory` 与节点语义一致，定义见 `NodeConfigTypeRegistry.cs`。
3. **禁止**新建非 `partial` 的 `NodeConfigTypeRegistry` 类；只能扩展已有 `partial`。

---

## 参考代码路径（本仓库）

| 用途 | 路径 |
|------|------|
| 节点基类与黑板 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/CustomNode/CustomNode.cs` |
| 运行时接口（Update/Stop/Reset） | `Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/Interface/Inner/IInnerRuntime.cs` |
| 创建配置、XML type 规则 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/Util/CLHelper.cs` |
| 注册表与 NodeCategory | `Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/Config/NodeConfigTypeRegistry.cs` |
| 根逻辑 Update/CanStop | `Assets/Script/HotUpdate/Framework/CoreGame/CustomLogic/CustomLogic.Base/CustomLogic.cs` |
| 行为基类 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/BehaviorNodes/Base/BehaviorNodeBase.cs` |
| 行为完整范例（单文件含 Cfg+Register+节点） | `Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/BehaviorNodes/Common/LogBhv.cs` |
| 条件基类 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/ConditionNodes/Base/ConditionNodeBase.cs` |
| 条件短范例 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/ConditionNodes/AwaysTrueCnd.cs` |
| 状态基类 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/FSMNodes/Base/StateNode.cs` |
| 带 Bhv/ExitBhv 的状态 | `Assets/Script/HotUpdate/Framework/CoreGame/CustomNodes.Foundation/FSMNodes/Common/CustomBhvState.cs` |

**目录习惯**：通用、与业务无关的节点 → `CustomNodes.Foundation`；玩法/业务状态或技能相关 → `Product/.../CustomNodes.Ex`（状态多继承 `CustomBhvState`）。

---

## 输出与代码风格

- `namespace HotUpdate.CoreGame`（除非目标目录已有不同约定且用户指定）。
- 错误日志优先 `CLHelper.LogError(this, "...")`（扩展方法，与现有节点一致）。
- 仅输出请求的文件内容；不要无关重构；不要删除文件内他人注册条目。

---

## 禁止与注意

- 覆写 `Destroy()` / `Reset()` 时**不要忘记** `base.Destroy()` / `base.Reset()`（若基类有可链式语义）。
- 子节点由 `CustomLogicFactory` 与父节点销毁逻辑回收；业务节点 **不要** 对已交给工厂的子节点再次 `DestroyCustomNode`，除非本地明确持有且文档约定如此。
- 「Destroy 后等价于刚 new」指**本类字段**已清空；池化对象会进入可复用状态，勿假设跨逻辑实例共享引用仍有效。

---

## 最小 XML 示例（Behavior，与 LogBhv 一致）

```xml
<Node type="LogBhv" LogStr="hello_custom_logic" />
```

对应配置类名 **`LogBhvCfg`**，且已 `Register(typeof(LogBhvCfg), NodeCategory.Bhv)`。

## 代码装配（code_only）提示

无 XML 时，在 C# 中 `new XxxCfg { ... }` 并挂到父配置的子节点列表即可；**仍须在某一 `.cs` 中完成 `Register`**，否则运行期无法按名创建（若从不走 `CreateNodeCfg` 字符串路径，仅代码 `new` + `NodeType()` 直建，则需确认调用链是否绕过注册表——本仓库常规路径均需注册）。
