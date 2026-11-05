# Product 知识库概要

索引：项目侧有哪些目录、玩法内容落在哪、从哪里扩展。范围是 `Assets/HotScripts/Product`。Product 与 Framework 同在热更程序集 `HotUpdate` 里。运行时环境是 `GEnvProduct`。

## 项目侧分层

| 位置 | 关键词 |
|------|--------|
| `Content/` | 玩法内容：逻辑配置、世界、UI、生成代码 |
| `GameEnv/` | `GEnvProduct`；环境状态 `ES_EnvInit`、`ES_Login`、`ES_Main`、`ES_Battle` |
| `Services/` | 项目服务与框架服务的产品侧登记 |
| `Modules/` | 框架模块的 partial，以及项目模块 |
| `Subassembly/` | 项目内可单独看待的玩法子程序 |

`ES_EnvInit` 进入后转到登录。`ES_Main` 打开 `UIMain`。`ES_Battle` 按世界配置 `MainWorldTest` 创建主世界。

| 目录 | 类型 | 关键词 |
|------|------|--------|
| `Services/SvcInput.Unity` | `IInputService` | 键鼠、虚拟轴、虚拟按钮。入口 `G.Input` |
| `Services/SvcLocalizationEx` | `LocalizationLanguageTable` | 系统语言到 `Language` 的映射 |
| `Services/SvcCustomLogicEx` | `CustomLogicService` partial | 登记逻辑容器；`BaseWarmup` 预热节点池 |
| `Modules/ModuleGMEx` | `ModuleGM` partial | GM 功能块目录：倍速、静态动作 |
| `Modules/ModuleDebugEx` | `ModuleDebug` partial | 调试模块的产品侧扩展位 |
| `Modules/ModuleDemo` | `ModuleDemo` | 演示模块。未标 `ModuleAutoRegister` |
| `Subassembly/CustomLogic.GamePlay` | `EntityVarCfg`、`PosVarCfg`、`AssetVarCfg` | 黑板变量的实体、坐标、资源读取 |
| `Subassembly/CustomLogic.UnitTest` | `TestCustomLogic` | 自定义逻辑的单元配置样例 |

## Content

根目录：`Assets/HotScripts/Product/Content/`。四块：`Gen` 生成物、`Logic` 玩法逻辑、`MainWorlds` 主世界、`UIScripts` 界面脚本。

### Gen

生成代码，随工具重写。

| 目录 | 关键词 |
|------|--------|
| `Gen/Luban` | Luban 表代码。运行时 `G.Tables`。JSON 在 `HotAssets/Config/Luban` |
| `Gen/ModelPoints` | 模型挂点访问代码 |

### Logic

自定义逻辑。一个玩法域通常是：

- `Configs`：该域的逻辑配置容器，继承 `LogicConfigBase`。模板与 Demo 分文件（`*.Demo.cs`）。
- `Nodes`：该域的逻辑类型和节点。

容器名与 ID 段记在 `LogicContainerKey`。`GEnvProduct` 创建逻辑服务后调用 `AddConfigContainers` 登记。黑板约定键在 `CvKey`：初始化时约好的可变值、节点占位键。静态配置留在 LogicConfig；运行时必备且不变的数据留在 LogicGenInfo。

节点取世界、实体、玩家：`CustomNodeContextExtensions`（`GetLogicWorld`、`GetMetaWorld`、`GetOwnerEntity`、`GetSourceEntity`、`GetOwnerPlayerInfo`）。

| 域 | 容器 | ID 段起点 | 逻辑类型 | 关键词 |
|----|------|-----------|----------|--------|
| `GameMode` | `CCN_GameMode` | 1000000 | `LogicGameMode` | 模式规则、模式状态机；持有关卡逻辑 |
| `GameLevel` | `CCN_Level` | 2000000 | `GameLevelLogic` | 关卡逻辑、关卡事件列表 |
| `GameLevel` 事件 | `CCN_LevelEvent` | 2900000 | `CustomLogic` | 关卡内事件逻辑 |
| `EntityFSM` | `CCN_EntityFSM` | 3000000 | `EntityMainFSMLogic` | 实体主状态机。状态：Born、Idle、Move、Die；Demo 状态另有 `*.Unity.cs` |
| `Skill` | `CCN_EntitySkill` | 4000000 | `SkillLogic` | 技能 |
| `AI` | `CCN_EntityAI` | 5000000 | `AILogic` | 实体 AI |
| `Subobject` | `CCN_EntitySubobject` | 6000000 | `SubobjectLogic` | 子物体 |
| `Buff` | `CCN_Buff` | 7000000 | `BuffLogic` | Buff 配置、节点、附着策略（`Policies`） |
| `Supply` | `CCN_Supply` | 8000000 | `SupplyLogic` | 补给 |

`GameMode` 的节点子目录：`ModeStates.Game`、`ModeStates.Demo`、`Bhv.Demo`。Demo 加载态含资源预热 `DemoModeLoading.PrewarmRes`。

### MainWorlds

产品主世界。类型 `MainWorlds` 继承 `LiteUnityWorlds`。世界条目在 `WorldsConfig`，当前条目键 `MainWorldTest`，组装在 `WorldsConfig.Main`。

| 目录 | 关键词 |
|------|--------|
| `Configs` | `WorldsConfig` 世界条目；`EntityConfigs` 实体配置位 |
| `CreationInfo` | `MainWorldCreationInfo`；玩家信息 `InGamePlayerInfo`、`PlayerFeaturesContext` |
| `System` | `SysGameplayInitialize_Main` 元组件与模式初始化；`SysDebugDemo` 调试 |
| `System/Profiler.Unity` | `SysDebugProfiler`、计时 |
| `LogicComponent/AttributesEx` | 属性分类体积、属性快照 |
| `LogicComponent/CommandEx` | 产品指令类型、指令前置处理 |
| `LogicComponent/ImmuneComponent` | 免疫：布尔、万分比、Buff、Buff 标签 |
| `LogicComponent/ImmuneEx` | 具体免疫种类 |
| `MetaComponent` | 玩家组件、时间缩放的产品侧扩展 |
| `Extension` | `EntityExtension`、`WorldExtension` |
| `Debug.Unity` | 实体调试：AI、Buff、主状态机、变量黑板 |

`MainWorldTest` 当前接上的系统关键词：基础包与 LiteUnity 包初始化、指令收发、模式更新、主状态机、Buff、视图加载与变换同步、生命与死亡、调试，以及 `SysGameplayInitialize_Main`。

### UIScripts

界面脚本在 `Content/UIScripts`。预制体、图和动画在 `HotAssets/Demo/UI`，不放进 Content。

一块界面以预制体名为准。这个名字同时是 YooAsset location、`UIPanelName` 常量和脚本子目录名。打开时 `G.UI.Show(UIPanelName.*)`，特性 `UIBaseHandler` 把类登记到这个名字上。

```text
Content/UIScripts/
  UIName.cs                 面板名、视图名
  ComponentEx/              各界面共用的控件
  Gen/Panel/                生成的 Panel 绑定，文件名 {预制体名}PanelGen.cs
  Gen/View/                 生成的 View 绑定，文件名 {视图名}ViewGen.cs
  UI/{预制体名}/Panel/      手写 Panel，{预制体名}Panel.cs
  UI/{预制体名}/View/       手写子 View，{视图名}View.cs

HotAssets/Demo/UI/
  UIPrefab/{预制体名}/{预制体名}.prefab
  Sprite/                   按界面分子目录，如 UIMain、UIBattle、UICommon
  Animation/
```

预制体节点前缀决定脚本落点：

| 节点前缀 | 落到 |
|----------|------|
| `t_` | 当前 Panel 或 View 的控件绑定 |
| `v_` | 一块子 View。去掉前缀后的名字加 `View` 为类名，脚本放进该界面的 `View/` |
| `c_` | 容器节点 |

手写文件已存在时生成器不覆盖。绑定生成在 `Gen/`，与手写类是同一个 `partial`。`Gen` 不按界面再分子目录。

现有界面对应：

| 预制体名 | 手写脚本 | 层级 |
|----------|----------|------|
| `UIMain` | `UI/UIMain/Panel/UIMainPanel`；视图 `MainTop`、`MainBottom` | `UILayerId.Panel` |
| `UIBattle` | `UI/UIBattle/Panel/UIBattlePanel` | `UILayerId.Panel` |
| `UIMsgList` | `UI/UIMsgList/Panel/UIMsgListPanel` | `UILayerId.Pop` |

`UIMsgItem` 是列表格子，手写在 `UI/UIMsgList/` 下，与 `Panel/` 平级，不走 View 生成。`ComponentEx` 放按钮、空图形、UV 滚动、滚动、遮罩、拖拽，不属于某一块界面。

## 扩展方式

Product 与 Framework 同一程序集，框架类型用 `partial` 在项目侧加成员。新的项目类型放在对应目录，由 `GEnvProduct` 或配置容器接上。

| 要加的东西 | 落点 |
|------------|------|
| 环境状态 | `GameEnv/EnvStates`，状态 ID 加到 `EnvStateID`，并在 `GEnvProduct.Inner_CreateManagers` 登记 |
| 新服务 | `Product/Services`，在 `GEnvProduct.Inner_CreateServices` 里 `AddService_*` |
| 已有服务的产品数据 | 该服务的 partial，或独立配置类（如 `LocalizationLanguageTable`） |
| 框架模块的产品行为 | `Product/Modules` 下同名 partial。GM 块在 `ModuleGM.RegisterCatalogBlocks` |
| 新模块 | `Product/Modules`。要自动进环境则标 `ModuleAutoRegister` |
| 新玩法逻辑域 | `Content/Logic/<域>/Configs` + `Nodes`；`LogicContainerKey` 增加容器名；`AddConfigContainers` 登记 |
| 已有域的一条逻辑 | 该域 Configs 里 `AddConfig` / 域专用登记；节点放 `Nodes` |
| 世界组件或系统 | `Content/MainWorlds` 对应目录，并在 `WorldsConfig` 的 `CreateRootSystems` 里加入 |
| 界面 | 预制体放 `HotAssets/Demo/UI/UIPrefab/{名}/`；手写脚本放 `UIScripts/UI/{名}/Panel` 与 `View`；`UIPanelName` 与预制体名相同；绑定生成进 `Gen/Panel`、`Gen/View` |
| 可独立的玩法工具类型 | `Product/Subassembly` |

## HotAssets

热更资源根：`Assets/HotAssets/`（`AppConfig.HotAssetFolderName`）。当前默认资源包名是 `PackDemoAsset`。

| 目录 | 内容 |
|------|------|
| `Config/App` | `language.json`，各语言自称 |
| `Config/Engine/DeviceProfile` | 设备档位（Windows、Android） |
| `Config/Luban` | Luban 导出的表 JSON |
| `Localization/<语言>/str_main` | 该语言的文本 JSON。现有 `en`、`zh_cn`、`zh_tw` |
| `Demo/Scenes` | 演示场景 `DemoHotScene` |
| `Demo/Prefabs` | 演示预制体、`VFX` |
| `Demo/UI/UIPrefab` | 界面预制体，按面板名分子目录 |
| `Demo/UI/Sprite`、`Animation` | 界面图、动画 |
| `Demo/Audio/Config` | Bank（`*.kab`）、`AudioMeta.json` |
| `Demo/Audio/Res` | 音频 Clip、Mixer |
| `Demo/Materials`、`Textures` | 演示材质与贴图，含 VFX |
| `Main/Prefabs/Actors` | 正式角色预制体 |
| `Dll_HotUpdate` | 热更程序集字节与 `HotUpdateDLLs.txt` |
| `Dll_PatchedAOT` | 需补充元数据的 AOT 程序集字节与 `AOTDLLs.txt` |
