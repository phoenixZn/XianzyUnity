# Luban 配表管线说明

本文说明本工程已落地的 Luban 客户端 JSON 管线：改表 → 生成代码/JSON → YooAsset RawFile → `G.Config.Tables`。

当前导出目标：`cs-simple-json` + `json`。官方文档：[datable.cn](https://www.datable.cn/)。工具版本：仓库内 `Tools/Luban`（Luban v5.1.0）。

---

## 1. 操作手册

### 1.1 环境

- 本机安装 **.NET SDK 8+**（生成脚本走 `dotnet Tools/Luban/Luban.dll`）。
- Unity 2022.3；Package Manager 能拉取 `com.code-philosophy.luban`（`Packages/manifest.json` 中 Gitee Git URL）。
- 首次打开工程若编译报找不到 `Luban` / `Luban.SimpleJSON`，等 UPM 完成 `luban_unity` 克隆后再刷新。

### 1.2 改表后重新生成

任选其一：

1. **Unity 菜单**：`HybridTool / 生成 Luban 配置`  
   内部执行 `DataTables/gen_client.bat`，成功后 `AssetDatabase.Refresh`。失败时 Console 会打出 Luban 退出码与 stderr。
2. **命令行**（仓库根目录）：

```bat
DataTables\gen_client.bat
```

生成写入：

| 产物 | 路径 |
|------|------|
| C# 代码 | `Assets/HotScripts/Product/Content/Gen/Luban/` |
| JSON 数据 | `Assets/HotAssets/Config/Luban/` |

生成代码与 JSON **需要提交**（其它同学不必本机跑 Luban 也能编过）。表源在 `DataTables/Data/`（Excel 或 CSV）。

根目录 `p4ignore.txt` 含 `**.xlsx`。Git 不忽略 xlsx。若走 Perforce，需给 `DataTables/` 开例外，否则表源进不了库。

### 1.3 运行时读取

`UnityGameEnv` 在 `AddService_Asset()` 之后调用 `AddService_Config()`。`TableConfigService` 用宿主注入的 loader 同步 `Init()`：按 `Tables` 构造器给出的文件名加载 JSON 再 `JSON.Parse`。

- Unity：`G.Asset.LoadAssetRawFileSync` 读 **PackMainScript** RawFile，文本拷贝后 `Release`。
- 命令行：`ConsoleGameEnv` 同样调用 `AddService_Config()`，从 `Assets/HotAssets/Config/Luban` 读同名 `.json`。

业务侧：

```csharp
var row = G.Config.Tables.某表.Get(id);
var orNull = G.Config.Tables.某表.GetOrDefault(0);
foreach (var item in G.Config.Tables.某表.DataList) { /* ... */ }
```

- `G.Config`：`IConfigService`（`Assets/HotScripts/Framework/Services/SvcConfig`）。
- `Tables`：生成类型，命名空间 `cfg`。
- YooAsset 地址规则为 **AddressByFileName**（无扩展名）。`Tables` 里 `loader("文件名")` 对应资源 `文件名.json`。

### 1.4 新增一张表（概要）

两条路径任选；嵌套结构、程序维护的类型优先 XML + CSV。

**A. XML schema + CSV**

1. 在 `DataTables/Defines/` 增加或改 XML：定义 bean / table，`input` 指向 CSV，`group` 含 `c`。
2. 在 `DataTables/Data/` 增加 CSV。第一列是行标记（`##var` / `##` / 数据行留空），其后才是字段；含逗号的单元格加引号。
3. 跑 `gen_client`。
4. 用 `G.Config.Tables.表名.Get(...)`；不要手改 `Gen/Luban`。

**B. Excel 元表 + `#` 文件**

1. 在 `DataTables/Data/` 增加 `#模块.表.xlsx`（`#` 前缀会自动登记表，不必手写 `__tables__` 行）。
2. 需要单独 bean / enum 时改 `__beans__.xlsx` / `__enums__.xlsx`。
3. 跑 `gen_client`。

Excel / CSV 列约定以 [Luban 文档](https://www.datable.cn/docs/beginner/quickstart) 为准：`##` 开头行为标记或注释。

表名与行 bean 不要同名（生成类都在 `cfg`，会冲突）。表类用 `Get` / `GetOrDefault` / `DataList`；行类型由 schema 的 `value` 决定。

### 1.5 日后切二进制（预留，当前不要默认跑）

- 脚本：`DataTables/gen_client_bin.bat`（`-c cs-bin -d bin`）。
- **不要**与 JSON 生成同时写到同一套 `outputCodeDir`。
- 运行时只改 `InstallSvc` 里的 loader（Unity 为 `LoadJsonFromAsset`）：同一 `LoadAssetRawFileSync`，改为 `new ByteBuf(handle.GetRawFileData())`，`Tables` 构造器参数类型会随生成代码变成 `ByteBuf`。命令行 loader 改为读 `.bytes`。
- 扩展名从 `.json` 变为 `.bytes`，AddressByFileName 仍是 `Tables` 里 `loader(...)` 的名字。

### 1.6 常见问题

| 现象 | 处理 |
|------|------|
| `dotnet` 找不到 / SDK 过低 | 安装 .NET 8+，命令行能跑 `dotnet --version` |
| 生成报 schema / 表定义错误 | 先看 `Defines/*.xml` 或 `__tables__.xlsx` 与 CSV/xlsx 是否对齐；看 Console 里 Luban 日志 |
| 编译找不到 `Luban` 命名空间 | 确认 `com.code-philosophy.luban` 已导入；`HotUpdate.asmdef` 已引用 `Luban.Runtime`（GUID `2a81c6962524d424a8ef5072bd3b0fa0`） |
| Play 时 load failed / empty | JSON 是否在 `HotAssets/Config/Luban`；收集器 CollectPath 是否指向该目录；地址是否等于 `Tables` 里 `loader("...")` 的名字。Simulate 读不到新 JSON 时，刷新收集器后再进 Play，或重打 **PackMainScript**。 |
| PureCsproj 编不过 Gen 代码 | 确认本机 `Library/PackageCache` 已有 `com.code-philosophy.luban`；`PureGameEnv.csproj` 编入该包 `Runtime` 与 `Gen/Luban` |

---

## 2. 目录结构与目录说明

表源、工具在 **Assets 外**（避免 Unity 导入 xlsx）。生成物进热更程序集与 RawFile 包。

```text
XianzyUnity/
├── Tools/Luban/                          # Luban 可执行目录（入口 Luban.dll）
├── DataTables/                           # 表工程：配置、schema、数据、生成脚本
│   ├── luban.conf
│   ├── Defines/                          # XML schema
│   ├── Data/                             # __tables__ / __beans__ / __enums__ / Excel 或 CSV
│   ├── gen_client.bat / gen_client.sh    # 客户端 JSON（默认）
│   └── gen_client_bin.bat / .sh          # 客户端二进制（预留）
├── Assets/
│   ├── Editor/Luban/                     # HybridTool 生成菜单
│   ├── HotScripts/
│   │   ├── HotUpdate.asmdef              # 引用 Luban.Runtime
│   │   ├── Framework/Services/SvcConfig/         # 运行时加载服务（Unity / CLI）
│   │   └── Product/Content/Gen/Luban/            # 生成的 C#（勿手改）
│   └── HotAssets/Config/Luban/           # 生成的 JSON，打进 PackMainScript
└── Packages/manifest.json                # com.code-philosophy.luban
```

| 路径 | 说明 |
|------|------|
| `Tools/Luban/` | 官方 Release 解压物。升级时替换此目录后重跑 `gen_client`。 |
| `DataTables/` | 唯一表工程根。`luban.conf` 的 `dataDir` 为 `Data`，`topModule` 为 `cfg`。 |
| `DataTables/Defines/` | XML schema。可与 Excel 元表并存。 |
| `DataTables/Data/` | 表数据与 Excel 元表。`__*.xlsx` 为 table/bean/enum 登记；业务数据为 xlsx 或 csv。 |
| `Assets/Editor/Luban/` | 仅 Editor；调用 `gen_client.bat`。 |
| `Assets/HotScripts/Framework/Services/SvcConfig/` | 配表服务。`TableConfigService` 只接收 loader；Unity 走 `G.Asset`，CLI 读 JSON 文件。 |
| `Assets/HotScripts/Product/Content/Gen/Luban/` | 生成代码，进 `HotUpdate`。与 `UIScripts/Gen` 同属 Product 生成物。 |
| `Assets/HotAssets/Config/Luban/` | 生成 JSON。YooAsset：`PackMainScript` / Other，`AddressByFileName` + `PackRawFile`，AssetTags=`luban`。 |
| `Packages` 中 `com.code-philosophy.luban` | 运行时 `ByteBuf`、`Luban.SimpleJSON`、`BeanBase`。 |

数据流：

```text
DataTables Excel/CSV  --gen_client-->  Gen/Luban C#  +  Config/Luban JSON
                                         |                    |
                                         v                    v
                                   HotUpdate / PureGameEnv    RawFile 或磁盘 JSON
                                         \                         /
                                          --> TableConfigService --> G.Config.Tables
```

---

## 3. 关键文件说明

### 3.1 `DataTables/luban.conf`

- `groups`：`c` 客户端 / `s` 服务端 / `e` 编辑器；当前生成 `-t client`，只导出带 `c` 的表。
- `schemaFiles`：`Defines` + 三张 `__*.xlsx`。
- `targets.client`：`manager=Tables`，`topModule=cfg` → 生成 `cfg.Tables`。

### 3.2 `DataTables/Defines/`

XML 适合稳定类型（公共 bean、嵌套结构、业务表定义）。根模块可空，类型进入 `cfg`。

`builtin.xml` 提供值类型 `vector2` / `vector3` / `vector4`（逗号分隔 float）。

### 3.3 `DataTables/Data/`

| 文件 | 作用 |
|------|------|
| `__tables__.xlsx` | 表登记（可空）。`#` 前缀数据文件会自动登记，不必手写行。 |
| `__beans__.xlsx` | bean 元表（可空）。也可全部写在 `Defines/*.xml`。 |
| `__enums__.xlsx` | 枚举定义。无枚举时也可留空表，文件需保留。 |
| 其它 xlsx / csv | 表数据。`#模块.名.xlsx` 由文件名推断模块与表；显式登记的表由 XML/`__tables__` 的 `input` 指向文件。 |

改数据：只改对应数据文件再生成。改字段：先改 schema（XML 或 `__beans__`），再改数据文件，再生成。

CSV 第一列是行标记，数据行该列留空；含逗号的单元格必须加引号。list / 嵌套 bean 可用 `sep` 写在一格内，写法见 [Luban 文档](https://www.datable.cn/docs/excel/nested-and-collections)。

### 3.4 生成代码与 JSON

| 产物 | 作用 |
|------|------|
| `Tables.cs` | 总入口。每张表一个属性；构造时 `loader("输出名")` 再 `ResolveRef`。 |
| 表类 | `DataList` / `DataMap` / `Get` / `GetOrDefault`。 |
| 行 bean / 嵌套 bean | 字段只读。`#ref` 会生成 `字段_Ref`，在 `ResolveRef` 后赋值。 |
| `*.json` | 与 `loader` 名同名（无扩展名）。有模块时一般为 `模块_表名` 小写；空模块一般为表名小写。 |

不要手改 `Gen/Luban`。YooAsset 地址等于该输出名。

### 3.5 运行时接入

| 文件 | 作用 |
|------|------|
| `SvcConfig/Standard/IConfigService.cs` | `Tables` / `Initialized` / `Init`。不关心数据来源。 |
| `SvcConfig/Impl/TableConfigService.cs` | 构造时注入 loader，`new Tables(loader)`。 |
| `SvcConfig/InstallSvc.cs` | `G.Config`、`AddService_Config()`。`CONSOLE_CLIENT` 读 JSON 文件，否则走 `G.Asset`。 |
| `Product/GameEnv/GEnvEx.Unity.cs` | `AddService_Asset()` 之后 `AddService_Config()`。 |
| `PureCsproj/.../GameEntry.Shim.cs` | `ConsoleGameEnv` 在协程服务之后 `AddService_Config()`。 |

业务以 `G.Config.Tables` 为准。
