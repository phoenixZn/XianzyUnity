# PureGameEnv 构建说明

## 位置

- 工程：`PureCsproj/PureGameEnv/PureGameEnv.csproj`
- UniTask 子集：`PureCsproj/UniTask/UniTask.csproj`（被 PureGameEnv `ProjectReference`）
- LitMotion 子集：`PureCsproj/LitMotion/LitMotion.csproj`（被 PureGameEnv `ProjectReference`）
- 解决方案（可选）：`PureCsproj/PureGameEnv.sln`

## 环境与目标框架

- 当前仓库所在机器仅有 **.NET SDK 5.0** 时，工程使用 **`net5.0`**，与 Unity 2022.3 常用的 netstandard2.1 语义兼容。
- 若已安装 **.NET 6+ SDK**，可将 csproj 中 `<TargetFramework>net5.0</TargetFramework>` 改为 `net6.0` 或 `net8.0` 后重新构建。

## Unity 托管引用（refs）

将 Unity 安装目录下 `Editor\Data\Managed\UnityEngine\` 中的下列文件拷贝到 `PureCsproj/PureGameEnv/refs/`（与 `ProjectSettings/ProjectVersion.txt` 一致为佳；本机曾用 2022.3.58f1 验证）：

- `UnityEngine.CoreModule.dll`
- `UnityEngine.SharedInternalsModule.dll`
- `UnityEngine.dll`

`refs/*.dll` 已加入根目录 `.gitignore`，需各环境自行拷贝。csproj 将这三份 DLL **复制到输出目录**（`Private=true`），以便 CLI 运行时 `Assembly.GetTypes` 能解析 `UnityEngine` 引用。

## 源码范围与排除

- **包含**：`Assets/HotScripts/Framework`、`Assets/HotScripts/Product` 下全部 `.cs`（通配 include）。
- **排除（与 XEditor.UnityPartingTool 语义一致）**：路径段以 `.Unity` 结尾的目录内所有 `.cs`；以及 `*.Unity.cs`。
- **排除 Editor**：任意路径段名为 `Editor` 的目录内所有 `.cs`（如 `ModelPointTool/Editor`）。CLI 不引用 `UnityEditor.dll`，`CustomEditor` / `MenuItem` 等编辑器 API 无法编译。
- `*.Unity.cs`、`.Unity` 目录与 `Editor` 目录已排除；命令行宿主由 `src/shim/GameEntry.Shim.cs` 提供 `GameEntry` / `ConsoleGameEnv`（不注册 Asset/Input/GOPool；协程服务注册 CLI 实现，由 `IEnvUpdate` 帧泵驱动，yield 仅支持 null / 嵌套 IEnumerator / handler 最小集）。
- LitMotion 源码在 `Assets/AOTScripts/ThirdParty/LitMotion`，不在 HotScripts 通配范围内；CLI 走 `PureCsproj/LitMotion` 的 `ProjectReference`。

## PureGameEnv 内 shim（未改 Unity 源码）

为在排除 `.Unity` 树后仍能编译，下列文件**仅存在于 PureGameEnv**，不向 Assets 拷贝：

| 文件 | 作用 |
|------|------|
| `src/shim/AsyncAssetViewWrapper.Shim.cs` | 替代 `YooAssetView.Unity/AsyncAssetViewWrapper`，供 View 包装器编译通过 |
| `src/shim/GameEntry.Shim.cs` | 替代 `GameEntry.Unity` / `GameEntryEx.Unity` / `GEnvEx.Unity`：命令行初始化 GEnv 并提供 FixedUpdate/Update/LateUpdate |

`SysDebugProfiler` 已迁到 `Assets/.../System/Debug/`，用 `#if CONSOLE_CLIENT` 提供空实现，不再需要工程内 shim。

## UniTask（Unity 无关子集）

`PureCsproj/UniTask` 引用 `Assets/Plugins/UniTask/Runtime` 中与 Unity / PlayerLoop 无关的源码，定义 `UNITASK_NETCORE`，不引用 `UnityEngine.*.dll`。NetCore 补丁（`Yield`、`IAsyncEnumerable` 互转）在 `PureCsproj/UniTask/NetCore/`，不改插件。

**可用**：`async UniTask` / `UniTaskVoid`、`WhenAll` / `WhenAny`、`UniTaskCompletionSource`、`SwitchToThreadPool`、`Run`、`Yield()`（线程池 / 同步上下文）、Linq（除 `Linq/UnityExtensions`）。

**不可用**：`Delay` / `WaitUntil` / `Yield(PlayerLoopTiming)` / `SwitchToMainThread` / Triggers 等依赖 PlayerLoop 的 API。CLI 主循环不会泵 UniTask 续体。

## LitMotion（数值 Tween 子集）

`PureCsproj/LitMotion` 引用 `Assets/AOTScripts/ThirdParty/LitMotion/Runtime`，排除 Extensions / PlayerLoop 注入 / Punch-Shake-String / Native 曲线 / Job。`src/shim/` 提供托管 `UpdateRunner`、空 Allocator、`UnsafeUtility.As` 的委托转换，以及 Burst / Mathematics / Collections 编译桩。

`GameEntry.Awake` 设置 `MotionScheduler.DefaultScheduler = MotionScheduler.Manual`，`Update` 中 `ManualMotionDispatcher.Default.Update(G.deltaTime)`（控制台 dt 固定 0.02）。

**可用**：`LMotion.Create` + `WithEase(Ease)` + `Bind` + `WithOnComplete`。

**不可用**：PlayerLoop 默认调度、`WithEase(AnimationCurve)`、`AddTo(GameObject)`、Extensions 绑定。

## 构建与运行

```text
dotnet build PureCsproj/PureGameEnv/PureGameEnv.csproj -c Debug
dotnet run --project PureCsproj/PureGameEnv/PureGameEnv.csproj
```

`Program` 会构造 `GameEntry` 并以 20ms 步长调用 FixedUpdate/Update/LateUpdate。Ctrl+C 后销毁 GEnv 并退出。无 GUI 时环境停在 `ES_Login`。

## 当前编译结果

- **已通过**：`dotnet build`（Debug）0 error。运行可见 `GameEntryInit` → Services/Modules → `ES_EnvInit` → `ES_Login`。

## 后续治理方向（按需）

- 为 `GEnv.Ex` / `IAssetService` 等增加纯逻辑抽象或 `Compile Remove` 更细粒度文件，而不是长期依赖 shim。
- CLI 运行时若调用 `Resources.Load` 等 API，需单独做资源加载抽象（编译期有 UnityEngine 不等于运行期可用）。
