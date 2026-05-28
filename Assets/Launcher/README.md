# Launcher 启动步骤状态机实现

## 概述

本实现严格按照《通用Launcher架构设计.md》文档设计，实现了基于 **string key** 的启动步骤状态机系统。

## 核心设计原则

1. **严格遵循文档设计**：所有类名、接口名、方法名、字段名均与文档保持一致
2. **使用 string key**：状态使用字符串ID标识，便于扩展和替换
3. **Launcher 作为唯一入口**：`Launcher : MonoBehaviour` 是唯一的 Unity 入口脚本
4. **状态机驱动**：通过 `Update()` 方法驱动状态机，不使用 `GameAsyncOperation` 包装

## 文件结构

```
Assets/Launcher/
├── FSM/
│   ├── ILauncherContext.cs      # 状态机上下文接口（严格按文档）
│   ├── LauncherFSM.cs            # 启动步骤状态机（严格按文档）
│   └── LauncherState.cs          # 状态基类（严格按文档）
├── States/
│   ├── LStateInitYooAsset.cs           # 初始化YooAsset资源系统
│   ├── LStateInitPackage.cs             # 初始化资源包
│   ├── LStateRequestPackageVersion.cs   # 请求资源版本
│   ├── LStateUpdatePackageManifest.cs   # 更新资源清单
│   ├── LStateCreateDownloader.cs        # 创建下载器
│   ├── LStateDownloadPackageFiles.cs    # 下载资源文件
│   ├── LStateDownloadPackageOver.cs     # 下载完成
│   ├── LStateClearCacheBundle.cs        # 清理缓存
│   └── LStateEndPatch.cs                # 补丁完成
├── Launcher.cs                   # 入口脚本（严格按文档）
├── Launcher.partial.cs           # 扩展实现（使用partial class）
└── README.md                      # 本文件
```

## 与文档的对应关系

### 1. 核心类（严格按文档）

| 文档中的类/接口 | 实现文件 | 说明 |
|---------------|---------|------|
| `ILauncherContext` | `FSM/ILauncherContext.cs` | 状态机上下文接口 |
| `LauncherFSM` | `FSM/LauncherFSM.cs` | 启动步骤状态机 |
| `LauncherState` | `FSM/LauncherState.cs` | 状态基类 |
| `Launcher` | `Launcher.cs` | Unity入口脚本 |

### 2. 方法签名（严格按文档）

- `LauncherFSM.Start(string dfaultStateID)` - 注意：保持文档中的拼写 `dfaultStateID`
- `LauncherFSM.FoeceChangeState(string nextStateID)` - 注意：保持文档中的拼写 `FoeceChangeState`
- `LauncherFSM.Update(float dt)` - 接收 `float dt` 参数
- `LauncherState.Update(float dt)` - 接收 `float dt` 参数
- `LauncherState.CheckTransitions()` - 返回下一个状态的 string ID

### 3. 字段（严格按文档）

- `LauncherFSM._states` - `Dictionary<string, LauncherState>`
- `LauncherFSM._currentState` - `LauncherState`
- `LauncherFSM._blackboard` - `Dictionary<string, object>`
- `LauncherFSM.OwnerMonoBhv` - `UnityEngine.MonoBehaviour`

## 状态ID命名规范

所有状态使用 `LS_` 前缀，遵循文档中的命名规范：

- `LS_InitYooAsset` - 初始化YooAsset资源系统
- `LS_InitPackage` - 初始化资源包
- `LS_RequestPackageVersion` - 请求资源版本
- `LS_UpdatePackageManifest` - 更新资源清单
- `LS_CreateDownloader` - 创建下载器
- `LS_DownloadPackageFiles` - 下载资源文件
- `LS_DownloadPackageOver` - 下载完成
- `LS_ClearCacheBundle` - 清理缓存
- `LS_EndPatch` - 补丁完成

## 状态类命名规范

所有状态类使用 `LState` 前缀：

- `LStateInitYooAsset`
- `LStateInitPackage`
- `LStateRequestPackageVersion`
- `LStateUpdatePackageManifest`
- `LStateCreateDownloader`
- `LStateDownloadPackageFiles`
- `LStateDownloadPackageOver`
- `LStateClearCacheBundle`
- `LStateEndPatch`

## 使用方式

### 1. 基本使用

```csharp
// 在场景中添加 Launcher 组件
// 设置 RuntimeSettings 和 PlayMode
// 状态机会自动启动
```

### 2. 扩展状态

```csharp
// 在 InitLaunchFSM 中添加新状态
_fsm.AddState("LS_CustomState", new LStateCustomState());
```

### 3. 替换状态

```csharp
// 使用相同的状态ID，替换实现
_fsm.AddState("LS_InitPackage", new CustomInitPackage());
```

## 设计特点

### 1. 异步支持

状态中可以使用 `UniTask` 进行异步操作，但 `Update(float dt)` 是缺省的驱动方式，用于：
- 检查运行状态
- 维护步骤的健壮性
- 处理状态转换

### 2. 状态转换机制

状态转换有两种方式：

1. **自动转换**：状态在 `CheckTransitions()` 中返回下一个状态的 ID
2. **事件触发**：通过事件系统调用 `FoeceChangeState(string stateID)`

### 3. 多包处理

支持处理多个资源包：
- 包列表存储在黑板中
- `LStateEndPatch` 自动检查是否有下一个包需要处理
- 通过状态机的自动转换机制处理下一个包

## 与旧实现的区别

| 特性 | 旧实现 (PatchLogic) | 新实现 (Launcher) |
|------|-------------------|-------------------|
| 入口 | `HybridLauncher` + `PatchOperation` | `Launcher` (唯一入口) |
| 状态标识 | 类型 (Type) | String Key |
| 驱动方式 | `GameAsyncOperation` | `Update()` 直接驱动 |
| 状态机 | `UniFramework.Machine.StateMachine` | `LauncherFSM` |
| 状态基类 | `IStateNode` | `LauncherState` |
| 命名空间 | 无 | `Launcher` |

## 注意事项

1. **保持文档设计**：所有与文档不一致的地方都已用注释标记
2. **拼写保持**：文档中的拼写（如 `dfaultStateID`、`FoeceChangeState`）已保持原样
3. **partial class**：扩展功能使用 `Launcher.partial.cs` 实现，保持原有设计不变
4. **事件系统**：事件处理在 `Launcher.partial.cs` 中实现

## 待确认事项

以下内容需要审核确认：

1. `Launcher.partial.cs` 中的扩展实现是否符合需求
2. 多包处理的逻辑是否需要调整
3. 是否需要添加其他启动步骤（如文档中提到的 `LS_InitApp`、`LS_LoadHotUpdateAssembly` 等）
