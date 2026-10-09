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

生成代码与 JSON **需要提交**（其它同学不必本机跑 Luban 也能编过）。Excel 表源在 `DataTables/Data/`。

根目录 `p4ignore.txt` 含 `**.xlsx`。Git 不忽略 xlsx。若走 Perforce，需给 `DataTables/` 开例外，否则表源进不了库。

### 1.3 运行时读取

`UnityGameEnv` 在 `AddService_Asset()` 之后调用 `AddService_Config()`，内部同步 `Init()`：按 `Tables` 构造器给出的文件名，从 **PackMainScript** RawFile 读文本，再 `JSON.Parse`。

业务侧：

```csharp
var item = G.Config.Tables.Tbitem.Get(1001);
var orNull = G.Config.Tables.Tbitem.GetOrDefault(0);
foreach (var row in G.Config.Tables.Tbitem.DataList) { /* ... */ }
```

- `G.Config`：`IConfigService`（`Assets/HotScripts/Framework/Services/SvcConfig.Unity`）。
- `Tables`：生成类型，命名空间 `cfg`。
- YooAsset 地址规则为 **AddressByFileName**（无扩展名）。`Tables` 里 `loader("demo_tbitem")` 对应资源 `demo_tbitem.json`。

### 1.4 验收

进 Play 后看日志：

- 配表服务：`[Config] Tables ready, Tbitem count=2, first=1001:道具1`
- `SysDebugDemo` 首帧：`TestLubanConfig ok, count=2, 1001:道具1`

若 Simulate 读不到新 JSON：刷新 YooAsset 收集器后再进 Play，或重打 **PackMainScript**。

### 1.5 新增一张表（概要）

1. 在 `DataTables/Data/__tables__.xlsx` 登记表（全名、值类型、input、分组 `c` 等），格式与现有 `demo.Tbitem` 一行同类。
2. 在 `DataTables/Data/` 增加数据 Excel（文件名习惯与 MiniTemplate 一致，如 `#模块.表.xlsx`）。
3. 需要新 bean / enum 时改 `__beans__.xlsx` / `__enums__.xlsx`。
4. 跑 `gen_client`。
5. 用 `G.Config.Tables.新表名.Get(...)`；不要手改 `Gen/Luban` 下文件（下次生成会覆盖）。

Excel 列约定以 [Luban 文档](https://www.datable.cn/docs/beginner/quickstart) 为准：表头含类型行、字段名行；`##` 开头行为注释。

### 1.6 日后切二进制（预留，当前不要默认跑）

- 脚本：`DataTables/gen_client_bin.bat`（`-c cs-bin -d bin`）。
- **不要**与 JSON 生成同时写到同一套 `outputCodeDir`。
- 运行时只改 `ConfigManager.LoadJson`：同一 `LoadAssetRawFileSync`，改为 `new ByteBuf(handle.GetRawFileData())`，`Tables` 构造器参数类型会随生成代码变成 `ByteBuf`。
- 扩展名从 `.json` 变为 `.bytes`，AddressByFileName 仍是表文件名（如 `demo_tbitem`）。

### 1.7 常见问题

| 现象 | 处理 |
|------|------|
| `dotnet` 找不到 / SDK 过低 | 安装 .NET 8+，命令行能跑 `dotnet --version` |
| 生成报 schema / 表定义错误 | 先看 `__tables__.xlsx` 与数据 xlsx 是否对齐；看 Console 里 Luban 日志 |
| 编译找不到 `Luban` 命名空间 | 确认 `com.code-philosophy.luban` 已导入；`HotUpdate.asmdef` 已引用 `Luban.Runtime`（GUID `2a81c6962524d424a8ef5072bd3b0fa0`） |
| Play 时 load failed / empty | JSON 是否在 `HotAssets/Config/Luban`；收集器 CollectPath 是否指向该目录；地址是否等于 `Tables` 里 `loader("...")` 的名字 |
| PureCsproj 编不过 Gen 代码 | 已 `Compile Remove` `Product/Content/Gen/Luban`；不要把 Luban 生成代码挪出该目录除非同步改 csproj |

---

## 2. 目录结构与目录说明

表源、工具在 **Assets 外**（避免 Unity 导入 xlsx）。生成物进热更程序集与 RawFile 包。

```text
XianzyUnity/
├── Tools/Luban/                          # Luban 可执行目录（入口 Luban.dll）
├── DataTables/                           # 表工程：配置、schema、Excel、生成脚本
│   ├── luban.conf
│   ├── Defines/                          # XML schema（内置向量等）
│   ├── Data/                             # Excel：__tables__ / __beans__ / __enums__ / 数据表
│   ├── gen_client.bat / gen_client.sh    # 客户端 JSON（默认）
│   └── gen_client_bin.bat / .sh          # 客户端二进制（预留）
├── Assets/
│   ├── Editor/Luban/                     # HybridTool 生成菜单
│   ├── HotScripts/
│   │   ├── HotUpdate.asmdef              # 引用 Luban.Runtime
│   │   ├── Framework/Services/SvcConfig.Unity/   # 运行时加载服务
│   │   └── Product/Content/Gen/Luban/            # 生成的 C#（勿手改）
│   └── HotAssets/Config/Luban/           # 生成的 JSON，打进 PackMainScript
└── Packages/manifest.json                # com.code-philosophy.luban
```

| 路径 | 说明 |
|------|------|
| `Tools/Luban/` | 官方 Release 解压物。升级时替换此目录后重跑 `gen_client`。 |
| `DataTables/` | 唯一表工程根。`luban.conf` 的 `dataDir` 为 `Data`，`topModule` 为 `cfg`。 |
| `DataTables/Defines/` | 非 Excel 的 schema。当前 `builtin.xml` 定义 `vector2/3/4`。 |
| `DataTables/Data/` | 表定义与策划数据。`__*.xlsx` 为元表；`#demo.item.xlsx` 为示例数据。 |
| `Assets/Editor/Luban/` | 仅 Editor；调用 `gen_client.bat`。 |
| `Assets/HotScripts/Framework/Services/SvcConfig.Unity/` | Unity 宿主配表服务（YooAsset）。目录名 `.Unity`，PureCsproj 不编入。 |
| `Assets/HotScripts/Product/Content/Gen/Luban/` | 生成代码，进 `HotUpdate`。与 `UIScripts/Gen` 同属 Product 生成物。 |
| `Assets/HotAssets/Config/Luban/` | 生成 JSON。YooAsset：`PackMainScript` / Other，`AddressByFileName` + `PackRawFile`，AssetTags=`luban`。 |
| `Packages` 中 `com.code-philosophy.luban` | 运行时 `ByteBuf`、`Luban.SimpleJSON`、`BeanBase`。 |

数据流：

```text
DataTables Excel  --gen_client-->  Gen/Luban C#  +  Config/Luban JSON
                                         |                    |
                                         v                    v
                                   HotUpdate 程序集     PackMainScript RawFile
                                         \                    /
                                          --> ConfigManager --> G.Config.Tables
```

---

## 3. 示例文件说明

示例来自官方 MiniTemplate 的 `demo.item` 表，用于打通管线，**不是**玩法 LogicConfig 的迁移。

### 3.1 `DataTables/luban.conf`

- `groups`：`c` 客户端 / `s` 服务端 / `e` 编辑器；当前生成 `-t client`，只导出带 `c` 的表。
- `schemaFiles`：`Defines` + 三张 `__*.xlsx`。
- `targets.client`：`manager=Tables`，`topModule=cfg` → 生成 `cfg.Tables`。

### 3.2 `DataTables/Defines/builtin.xml`

内置值类型 `vector2` / `vector3` / `vector4`（逗号分隔 float）。生成对应 `cfg/vector2.cs` 等。示例道具表未使用向量字段。

### 3.3 `DataTables/Data/` 下 Excel

| 文件 | 作用 |
|------|------|
| `__tables__.xlsx` | 登记有哪些表。示例行对应全名 `demo.Tbitem`、数据文件 `#demo.item.xlsx`、分组含 `c`。 |
| `__beans__.xlsx` | bean 定义。示例 `demo.item`：`id, name, desc, count`。 |
| `__enums__.xlsx` | 枚举定义。示例工程可为空表，仍需保留文件。 |
| `#demo.item.xlsx` | 示例数据。当前两行：id `1001`「道具1」、id `1002`「道具2」。文件名 `#` 前缀为 Luban 模块/表命名习惯。 |

改示例数据：只改 `#demo.item.xlsx` 再生成。改字段：先改 `__beans__.xlsx`（及 `__tables__` 如有需要），再改数据表，再生成。

### 3.4 生成代码（`Product/Content/Gen/Luban`）

| 文件 | 作用 |
|------|------|
| `Tables.cs` | 总入口。构造时 `loader("demo_tbitem")`，属性 `Tbitem`。 |
| `demo/Tbitem.cs` | 表容器：`DataList` / `DataMap` / `Get` / `GetOrDefault`。主键为 `id`（int）。 |
| `demo/item.cs` | 行类型：`Id`, `Name`, `Desc`, `Count`。 |
| `vector2.cs` 等 | 对应 `builtin.xml`，可忽略若业务不用。 |

`demo_tbitem` 由模块名 `demo` + 表名 `Tbitem` 拼出，与 JSON 文件名一致。

### 3.5 生成数据 `demo_tbitem.json`

JSON 数组，字段小写，与 Excel 列对应。YooAsset 地址：`demo_tbitem`。

### 3.6 运行时与调试示例

| 文件 | 作用 |
|------|------|
| `SvcConfig.Unity/Standard/IConfigService.cs` | `Tables` / `Initialized` / `Init`。 |
| `SvcConfig.Unity/Impl/ConfigManager.cs` | RawFile → JSON → `new Tables(LoadJson)`。切 bin 只改此处 loader。 |
| `SvcConfig.Unity/InstallSvc.cs` | `G.Config`、`AddService_Config()`。 |
| `Product/GameEnv/GEnvEx.Unity.cs` | `AddService_Asset()` 之后 `AddService_Config()`。 |
| `SysDebugDemo.cs` | `partial void TestLubanConfig()`；CLI 无实现则空操作。 |
| `SysDebugDemo.Config.Unity.cs` | Unity 下校验行数、`Get(1001)` 字段、缺失键 `GetOrDefault`。 |

业务新代码不要依赖 `SysDebugDemo` 里的样例常量；以 `G.Config.Tables` 为准。
