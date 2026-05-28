# PureCsproj

Unity 主工程（`Assets/`）的**附加工程目录**。从纯 C# 命令行工程的视角，筛选并引用主工程中的源码，用于在 **不启动 Unity Editor、不加载游戏资源** 的前提下，单独编译与验证**引擎无关、资源无关**的业务逻辑。

## 为什么需要这个目录

Unity 工程编译依赖 Editor 管线、程序集定义与大量 Unity 特有 API，纯逻辑代码的编译错误往往要到进 Editor 或打热更包时才会暴露。PureCsproj 提供一条轻量的 `dotnet build` 路径：

- **源码仍留在 `Assets/HotScripts/`**，不在此目录复制业务代码，避免双份维护。
- **筛选规则与 Unity 侧 UnityParting 约定一致**（排除 `.Unity` 目录与 `*.Unity.cs`），保证「纯 C# 视图」与主工程分层语义对齐。
- **编译期 shim 与最小 Unity 引用**仅存在于本目录，不污染 Unity 源码树。

当前已有一个子工程 `PureGameEnv/`；后续可按同样约定在此目录下继续添加其他纯 C# 验证工程。

## 目录结构

```text
PureCsproj/
├── Readme.md                 # 本说明
├── PureGameEnv.sln           # 可选，IDE 打开用
└── PureGameEnv/
    ├── PureGameEnv.csproj    # 主 csproj
    ├── Program.cs            # CLI 入口（冒烟编译 / 运行）
    ├── *.Shim.cs             # 仅本工程使用的编译期垫片
    └── refs/                 # 本机 Unity 托管 DLL（不进 git，需自行拷贝）
```

## csproj 约定

以下为 PureCsproj 下各子工程应遵循的通用模式（以 `PureGameEnv.csproj` 为参考）。

### 1. 工程类型与语言版本

| 项 | 约定 |
|----|------|
| SDK | `Microsoft.NET.Sdk` |
| 输出 | 控制台 `Exe`（或按需改为 `Library`） |
| 目标框架 | 默认 `net5.0`（与 Unity 2022.3 / netstandard2.1 语义兼容）；本机有 .NET 6+ 时可改为 `net6.0` / `net8.0` |
| 语言 | `LangVersion` 9.0，与 Unity 侧 C# 9 对齐 |
| 其他 | `EnableDefaultCompileItems=false`；按需 `AllowUnsafeBlocks` |

若本机仅有 **.NET SDK 5.0**，保持 `net5.0` 即可；安装 **.NET 6+ SDK** 后，可将 csproj 中 `<TargetFramework>net5.0</TargetFramework>` 改为 `net6.0` 或 `net8.0` 后重新构建。

### 2. 源码引用：通配 include + 显式 exclude

**不**把 `Assets` 下的文件复制进 PureCsproj，而是通过相对路径 **Compile Include** 引用主工程源码，并用 **Link** 在 IDE 中呈现虚拟目录结构：

```xml
<Compile Include="$(MSBuildThisFileDirectory)..\..\Assets\HotScripts\Framework\**\*.cs">
  <Link>Framework\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Compile>
<Compile Include="$(MSBuildThisFileDirectory)..\..\Assets\HotScripts\Product\**\*.cs">
  <Link>Product\%(RecursiveDir)%(Filename)%(Extension)</Link>
</Compile>
```

**包含范围**（PureGameEnv 当前配置）：

- `Assets/HotScripts/Framework`、`Assets/HotScripts/Product` 下全部 `.cs`（通配 include）

**排除规则**（与 `XEditor.UnityPartingTool` 语义一致）：

- 路径段以 `.Unity` 结尾的目录内所有 `.cs`
- 文件名（去扩展名）以 `.Unity` 结尾的 `*.Unity.cs`
- 额外 `Compile Remove`：仍依赖 Unity 资源 / 第三方 Unity 程序集、且短期无法用抽象替代的单文件  
  - 当前示例：`Assets/HotScripts/Product/GEnv.Ex.cs`（依赖 `Services.Unity` 中的 `AssetService` / `AppConfig`，不引入 YooAsset 等程序集）

新增子工程时，include 范围按模块调整，但 **exclude 规则应保持一致**，避免把 Unity 绑定代码拉进纯 C# 编译。

### 3. 本工程自有文件与 Shim

除 **Program.cs**（或测试入口）外，仅在此目录放置 **Shim**（编译期最小实现，替代被 exclude 的 Unity 侧代码）：

- 命名建议 `*.Shim.cs`，**不要**写回 `Assets/`
- 在 csproj 中显式 `<Compile Include="...Shim.cs" />`



### 4. Unity 程序集引用（refs）

将 Unity 安装目录 `Editor\Data\Managed\UnityEngine\` 中的下列文件拷贝到 `PureGameEnv/refs/`（版本与 `ProjectSettings/ProjectVersion.txt` 一致为佳）：

- `UnityEngine.CoreModule.dll`
- `UnityEngine.SharedInternalsModule.dll`
- `UnityEngine.dll`


### 5. 构建产物与版本控制

- 忽略：`bin/`、`obj/`、`refs/*.dll`、`refs/*.pdb`
- 纳入版本控制：`.csproj`、`.sln`、`Program.cs`、Shim 源码

