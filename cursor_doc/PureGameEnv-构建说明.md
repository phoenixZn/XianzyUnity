# PureGameEnv 构建说明

## 位置

- 工程：`PureCsproj/PureGameEnv/PureGameEnv.csproj`
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
- `*.Unity.cs` 与 `.Unity` 目录已排除；命令行宿主由 `src/shim/GameEntry.Shim.cs` 提供 `GameEntry` / `ConsoleGameEnv`。

## PureGameEnv 内 shim（未改 Unity 源码）

为在排除 `.Unity` 树后仍能编译，下列文件**仅存在于 PureGameEnv**，不向 Assets 拷贝：

| 文件 | 作用 |
|------|------|
| `src/shim/AsyncAssetViewWrapper.Shim.cs` | 替代 `YooAssetView.Unity/AsyncAssetViewWrapper`，供 View 包装器编译通过 |
| `src/shim/GameEntry.Shim.cs` | 替代 `GameEntry.Unity` / `GameEntryEx.Unity` / `GEnvEx.Unity`：命令行初始化 GEnv 并提供 FixedUpdate/Update/LateUpdate |

`SysDebugProfiler` 已迁到 `Assets/.../System/Debug/`，用 `#if CONSOLE_CLIENT` 提供空实现，不再需要工程内 shim。

## 构建与运行

```text
dotnet build PureCsproj/PureGameEnv/PureGameEnv.csproj -c Debug
dotnet run --project PureCsproj/PureGameEnv/PureGameEnv.csproj
```

`Program` 会构造 `GameEntry` 并以 20ms 步长调用 FixedUpdate/Update/LateUpdate。Ctrl+C 后销毁 GEnv 并退出。无 GUI 时环境停在 `ES_Login`。

## 当前编译结果

- **已通过**：`dotnet build`（Debug）0 error；冒烟运行可见 `GameEntryInit` → Services/Modules → `ES_EnvInit` → `ES_Login`。

## 后续治理方向（按需）

- 为 `GEnv.Ex` / `IAssetService` 等增加纯逻辑抽象或 `Compile Remove` 更细粒度文件，而不是长期依赖 shim。
- CLI 运行时若调用 `Resources.Load` 等 API，需单独做资源加载抽象（编译期有 UnityEngine 不等于运行期可用）。
