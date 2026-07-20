# ViewComponent + ViewWrapper 设计文档

## 1. 设计目标

`ViewComponent` 将逻辑 Entity 与引擎渲染对象解耦，使逻辑层在以下场景下使用同一套 API：

- Unity 运行时：异步加载 Prefab，绑定真实 `Transform`
- 命令行 / 纯逻辑：通过 `NullViewTransformProxy` 空实现，不依赖引擎

核心原则：**逻辑侧始终同步调用**，资源加载异步进行，由 ViewWrapper 的 Shadow State 缓冲加载完成前的变换数据。

## 2. MVVM 映射

| 角色 | 类型 | 职责 |
|------|------|------|
| Model | `TransformComponent` | 逻辑层权威位置、朝向、缩放 |
| ViewModel | `IViewWrapper` / `IAssetViewLoadable` / `IViewTransformSyncable` / `ViewWrapperBase` | 多 Wrapper 组合；按需加载与 Transform 同步 |
| View | `IViewTransformProxy` 实现 | 操作引擎 `Transform`；无引擎时用 Null 实现 |

```mermaid
flowchart LR
    subgraph logic [LogicLayer]
        Entity[LogicEntity]
        TC[TransformComponent]
    end
    subgraph vm [ViewModelLayer]
        VC[ViewComponent]
        VW[IViewWrapper]
        AL[IAssetViewLoadable]
        TS[IViewTransformSyncable]
    end
    subgraph engine [EngineLayer]
        Proxy[IViewTransformProxy]
        GO[Unity Transform]
    end
    Entity --> TC
    Entity --> VC
    VC -->|"List + CacheInterface"| VW
    VW -.->|可选实现| AL
    VW -.->|可选实现| TS
    VW --> Proxy
    Proxy --> GO
    SysSync[SysSyncViewTransform] -->|"读 position/rotation"| TC
    SysSync -->|"ApplyTransform"| TS
    SysLoad[SysViewLoader] -->|"Load + BindProxy"| AL
```

## 3. 类型职责与生命周期

### ViewComponent

- 挂载于 `LogicEntity`，持有 `List<IViewWrapper>`（对齐 CustomLogic 的多节点模式）
- `CacheInterface`：按接口分流缓存（`IAssetViewLoadable`、`IViewTransformSyncable`，便于按接口区分输入响应）
- `Init(IViewWrapper)`：无 location；可选加入第一个 Wrapper
- `AddViewWrapper`：追加 + `CacheInterface` + `NotifyChanged`
- `MarkLoading` / `MarkReady` / `MarkFailed`：针对具体 `IAssetViewLoadable`
- `DisposeOnRemove`：释放全部 Wrapper 并清空接口缓存

### IViewWrapper

逻辑层访问表现的基础入口：

- `BindProxy`：绑定 `IViewTransformProxy` 并 `FlushToProxy`
- `SetActive`：显隐控制（同样走 Shadow + Flush）

### IAssetViewLoadable

表达「需要按 `AssetLocation` 异步加载」的能力（与表现 API 解耦）：

- `AssetLocation`：资源路径（由 `RequestLoad` 写入）
- `LoadState`：`None` → `Loading` → `Ready` / `Failed`
- `RequestLoad(string)`：写入路径并重置为 `None`，触发 Loader
- `SetLoadState`：由 System / Component 推进状态

### IViewTransformSyncable

表达「需要同步逻辑 Transform 到表现」的能力：

- `SyncTransform`：是否参与同步（默认 `true`，可临时关闭）
- `ApplyTransform`：写入 Shadow，Proxy 就绪时立即刷到引擎

### ViewWrapperBase

同时实现 `IViewWrapper`、`IAssetViewLoadable`、`IViewTransformSyncable` 的默认实现。

### IViewTransformProxy

引擎 Transform 的抽象，隔离 Unity 依赖：

- `UnityViewTransformProxy`：包装 `UnityEngine.Transform`
- `NullViewTransformProxy`：纯逻辑空实现，`IsValid = true`

### 生命周期

1. `SetComView(assetLocation)` → 确保组件存在 → 对 Wrapper 调用 `IAssetViewLoadable.RequestLoad` → `AddViewWrapper`
2. `SysViewLoader` 响应 Added/Replaced → 遍历待加载 `AssetLoadables` → `Loading` → 异步加载
3. 加载成功 → `Instantiate` → `BindProxy` → `Ready` → 刷入当前 `TransformComponent`
4. Entity 销毁 / 组件移除 → `DisposeOnRemove` → 销毁 GameObject、释放资源

## 4. Shadow State + Pending Flush

`ViewWrapperBase` 维护 `_position`、`_rotation`、`_scale`、`_active`：

1. `ApplyTransform` / `SetActive` **立即**更新 Shadow，不等待加载
2. 若 Proxy 已绑定且 `IsValid` → 同步写入 Proxy
3. 资源加载中 → 仅更新 Shadow
4. `BindProxy` 完成后调用 `FlushToProxy()`，一次性应用缓存

```mermaid
sequenceDiagram
    participant Logic
    participant VC as ViewComponent
    participant VW as ViewWrapper
    participant Loader as SysViewLoader
    participant Sync as SysSyncViewTransform
    participant Proxy as IViewTransformProxy

    Logic->>VC: SetComView("prefab/path")
    VC->>VW: RequestLoad then AddViewWrapper
    Loader->>VW: async load
    Logic->>Sync: SetPosition via TransformComponent
    Sync->>VW: ApplyTransform Shadow only
    Loader->>Proxy: Instantiate + BindProxy
    VW->>Proxy: FlushToProxy
    Logic->>Sync: SetRotation
    Sync->>VW: ApplyTransform
    VW->>Proxy: immediate write
```

## 5. 纯逻辑 / 命令行运行

当 `GEnv.Inst.Services.AssetSvc` 不可用时，`SysViewLoader` 直接：

1. 绑定 `NullViewTransformProxy`
2. 标记 `Ready`
3. `FlushToProxy`（无引擎副作用）

逻辑代码路径与 Unity 运行时一致，无需分支判断。

## 6. System 协作

### SysViewLoader（ReactiveSystem）

- **Trigger**：`ComView` Added（含 Replace 触发的 Added）
- **Filter**：`hasComView && HasPendingAssetLoad`
- **Execute**：遍历 `AssetLoadables` 中 `LoadState == None` 且路径非空的项 → Loading → 异步加载 → BindProxy → Ready；失败则 Failed

### SysSyncViewTransform（ReactiveSystem）

- **Trigger**：`ComTransform` Added
- **Filter**：`hasComTransform && hasComView && HasSyncTransform`
- **Execute**：读取 `position` / `rotation` / `scale`，对 `TransformSyncables` 中 `SyncTransform == true` 的项调用 `ApplyTransform`

朝向同步使用 `TransformComponent.rotation`（与 `SetFaceDir` 派生的 `WorldFaceDir` 一致）。

加载完成时，`SysViewLoader` 会额外调用一次 `ApplyTransform`，保证首帧与逻辑 Transform 对齐。

## 7. 使用示例

```csharp
var entity = logicWorld.CreateEntity();
entity.SetComTransform(Vector3.zero, Quaternion.identity, Vector3.one);
entity.SetComView("Characters/Hero");

// 追加第二个可加载 Wrapper
entity.SetComView("Characters/HeroWeapon");

// 逻辑移动 — 加载前后均可调用
entity.SetPosition(new Vector3(1, 0, 0));
entity.SetFaceDir(Vector3.forward);
```

## 8. 扩展点

- **自定义 Wrapper**：`ViewComponent.AddViewWrapper(customWrapper)` 注入测试或特化实现；在 `CacheInterface` 中按接口分流
- **SysSyncViewAnimator**：后续可按相同 Reactive 模式同步动画参数
- **父节点挂载**：可在 `SysViewLoader` 实例化后设置 `SetParent`，或扩展 `IViewTransformProxy`
