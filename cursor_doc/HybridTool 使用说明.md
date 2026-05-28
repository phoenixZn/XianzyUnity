# HybridTool / HybridBuilder 使用说明

本文档面向工程内 **Assets/Editor** 下定制的 **HybridTool** 与 **Hybrid Builder** 体系，说明各菜单与窗口按钮的**内部实现概要**及**推荐使用时机**。官方 HybridCLR 包自带的 `HybridCLR/...` 菜单不在此展开，仅在流程中引用。

---

## 1. 架构总览

| 模块 | 作用 |
|------|------|
| `HybridBuilderWindow` | 编辑器窗口：选择 `HybridBuilderSettings` / `HybridRuntimeSettings`，展示构建 UI并触发打包。 |
| `HybridBuilderSettings` | 构建配置：输出路径、版本号、YooAsset 包、脚本包、AOT/热更 DLL 收集目录、混合构建模式等。 |
| `HybridBuildPipeViewerBase` | UI Toolkit基类：输出目录、版本、加密/压缩、首包拷贝、AOT/热更文件夹、`HybridBuildOption`、Build 按钮。 |
| `HybridScriptableBuildPipelineViewer` | 实际执行构建：校验 → `StartBuild` → `BuildFinish`（写 `RuntimeSettings.json`、递增版本）。 |
| `HybrdiScriptableBuildPipeline` | 针对**脚本/Raw 包**的 YooAsset 管线：在标准 RawFile任务链中插入 `TaskBuildScript_SBP`。 |
| `TaskBuildScript_SBP` | 构建前：编译热更 DLL → 将 AOT 补充 DLL 与热更 DLL 复制到 Collector 指向的目录。 |
| `BuildHelper` | `HybridTool/*` 菜单与 APK 构建、元数据校验、DLL 复制、link.xml 补全等工具方法。 |

---

## 2. Hybrid Builder 窗口（菜单：`HybridTool/Hybrid Builder`）

### 2.1 打开方式与前置条件

- **实现**：`HybridBuilderWindow.OpenWindow()` 注册为 `MenuItem("HybridTool/Hybrid Builder", false, 102)`。
- **前置**：
  - 工程中至少存在一个 **`HybridBuilderSettings`** 资源（`AssetDatabase.FindAssets("t:HybridBuilderSettings")`）。
  - 至少存在一个 **`HybridRuntimeSettings`** 资源。
  - YooAsset 的 **`AssetBundleCollectorSettingData`** 中配置的 **Package 数量 ≥ 2**；否则 `HybridBuildPipeViewerBase` 会因「少于两个包」直接 `return false`，界面无法完整创建（会显示 `PackageErrorLabel`）。

### 2.2 工具栏两个下拉菜单

1. **HybridBuilderSettings 选择**  
   - 切换当前使用的构建配置资产，切换后刷新下方管线视图。

2. **HybridRuntimeSettings 选择**  
   - 绑定到当前 `HybridBuilderSettings.RuntimeSettings`；构建结束时会更新该资产中的 `Packages` JSON、`ReleaseBuildVersion` 等，并写出 `RuntimeSettings.json`。

### 2.3 主界面「构建」按钮（内部流程）

1. 确认对话框后 `EditorApplication.delayCall += ExecuteBuild`。
2. **`HybridScriptableBuildPipelineViewer.ExecuteBuild()`** 按 `hybridBuildOption` 分支校验：
   - **BuildScript / BuildAll**：调用 `BuildHelper.CheckAccessMissingMetadata()`；失败则提示应走 **Build Application**（见下文「元数据校验」）。
   - **含脚本或全量/出包**：`CheckScriptPathExsist()` —— 要求 **Script Package** 对应的 YooAsset Collector 中，必须包含与 `PatchedAOTDLLFolder`、`HotUpdateDLLFolder` **GUID 一致**的收集项。
   - **BuildAsset / BuildAll / BuildApplication**：`AssetPackages` 非空。
3. **`StartBuild()`** 按模式组合：
   - **BuildAsset**：仅对每个勾选的资源包执行 `BuildAsset`（YooAsset `ScriptableBuildPipeline` + AssetBundle）。
   - **BuildScript**：仅 `BuildScriptPackage()`（Raw 管线 + `TaskBuildScript_SBP`）。
   - **BuildAll**：对每个资源包先 `BuildScriptPackage()` 再 `BuildAsset(package)`（保证脚本 Raw 先于 AB）。
   - **BuildApplication**：先 `BuildHelper.BuildAPK(...)`（当前仅 **Android** 分支有实现），再脚本包 + 各资源包。
4. **`BuildFinish()`**：合并/更新运行时包版本字典，递增 `AssetBuildVersion` / `ScriptBuildVersion` / `ReleaseBuildVersion`（按模式），序列化到 `RuntimeSettings`，并写入 **`buildOutputPath/RuntimeSettings.json`**，最后在输出目录 `RevealInFinder`。

### 2.4 `HybridBuildOption` 何时选用

| 选项 | 含义 | 建议使用时机 |
|------|------|----------------|
| **None** | 不构建 | 仅查看配置；点 Build 时 `ExecuteBuild` 直接 return。 |
| **BuildAsset** | 只打热更资源 AB | 仅资源变更、不需要重新编译/分发 DLL；需已勾选至少一个非脚本资源包。 |
| **BuildScript** | 只打「脚本/Raw」包（内含 AOT 补充 + 热更 DLL） | 只改代码、不改资源；**且** `CheckAccessMissingMetadata` 必须通过（即与当前主包裁切后的 AOT 一致）。若曾改 AOT 边界或首次未出包，应先 **Build Application** 或按流程更新 AOT DLL。 |
| **BuildAll** | 每个选中的资源包：先脚本 Raw 再打该包 AB | 一次发布中代码与资源都变；同样需要元数据校验通过。 |
| **BuildApplication** | 打 **APK**（Android）+ 脚本包 + 资源包 | **新主包/换 HybridCLR 或 il2cpp 裁剪结果**；会触发完整 `BuildHelper.BuildAPK` 流程（见下）。非 Android 时 `BuildApplication()` 返回 `false`，需扩展代码才能支持其他平台。 |

---

## 3. `HybridTool` 菜单项（`BuildHelper`）

### 3.1 `HybridTool/验证元数据是否需要补充`

- **实现**：`MissingMetadataChecker`，用 `SettingsUtil.GetAssembliesPostIl2CppStripDir(target)` 下裁切后的 AOT DLL，对比热更程序集（`SettingsUtil.HotUpdateAssemblyFilesExcludePreserved`）。
- **使用时机**：
  - 在发布 **BuildScript / BuildAll** 前自检；
  - 注释中说明：若刚跑过会刷新 strip 目录的流程，可能**掩盖**裁剪问题；可靠对比应基于「上次主包构建时保存的 AOT」——工程内通过流程上「先出主包再热更」来保证。

### 3.2 `HybridTool/打APK包`

- **实现**：`Debug_BuildAPK()` → `BuildAPK(ProjectPath/Bundles, "9999")`。
- **内部要点**（`BuildAPK`）：
  - `Il2CppDefGeneratorCommand.GenerateIl2CppDef()`、`LinkGeneratorCommand.GenerateLinkXml()`；
  - `SupplementPrefabDependent()`（与「补全热更新预制体依赖」同源）；
  - `GetBuildScenes()` 会实例化 `HotUpdateLauncher` 并调用 `test()`（用于场景列表侧逻辑，需知悉有该副作用）；
  - 成功则 `GetPatchedAOTAssemblyListToHybridCLRSettings()`、`MethodBridgeGeneratorCommand.GenerateMethodBridgeAndReversePInvokeWrapper()`。
- **使用时机**：**调试/快速出 APK**，输出路径与版本写死为示例值；正式发版更建议用 **Hybrid Builder → BuildApplication** 以使用 `HybridBuilderSettings` 中的输出路径与版本。

### 3.3 `HybridTool/获取需要补充元数据的Dll`

- **实现**：`CompileDllCommand.CompileDllActiveBuildTarget()` → `GetPatchedAOTAssemblyListToHybridCLRSettings()`（`AssemblyReferenceDeepCollector` + `Analyzer`，结果写入 `HybridCLRSettings.patchAOTAssemblies`）。
- **使用时机**：在**已能正确编译热更 DLL** 的前提下，刷新「需要补充元数据的 AOT 程序集列表」，供 link/出包或拷贝流程使用；**不**替代完整主包构建。

### 3.4 `HybridTool/生成AOT补充文件并复制进文件夹`

- **实现**：`GenerateIl2CppDef` → `GenerateLinkXml` → `StripAOTDllCommand.GenerateStripedAOTDlls()`，再 `CopyPatchedAOTDllToCollectPath(Application.dataPath/HotAssets/PatchedAOTDLL)`，并写 `AOTDLLs.txt`。
- **使用时机**：需要**不经过 Hybrid Builder**时，手动刷新 **strip 后的 AOT DLL** 到收集目录（例如仅调试 Collector）；注意与 HybridCLR 官方 **Generate/AOTDlls** 流程的依赖关系（注释中已说明）。

### 3.5 `HybridTool/生成热更新Dll并复制进文件夹`

- **实现**：编译热更 DLL → `CopyHotUpdateDllToCollectPath(.../HotAssets/HotUpdateDLL)`，写 `HotUpdateDLLs.txt`。
- **使用时机**：仅更新热更程序集到固定目录，供 YooAsset 收集或本地验证；**与窗口内 BuildScript 的关系**：窗口构建时 `TaskBuildScript_SBP` 内也会 `CompileDll` + `CopyHotUpdateDllToCollectPath`，通常日常以 **Builder 一键** 为主，本菜单适合**单独拷 DLL**。

### 3.6 `HybridTool/补全热更新预制体依赖`

- **实现**：扫描 `Assets/HotAssets` 下 Prefab 引用的 Unity/TMPro 类型，与 `HybridCLRData/Generated/link.xml` 合并，**输出到 `Assets/link.xml`** 并 `Refresh`。
- **使用时机**：热更资源里 Prefab 使用了易裁剪的 Unity 类型，需在 **打主包前** 扩充 link，减少 il2cpp 裁切导致的热更缺失元数据；`BuildAPK` 内也会调用 `SupplementPrefabDependent()`。

---

## 4. 脚本 Raw 管线核心：`TaskBuildScript_SBP`

构建脚本包时，在 YooAsset 任务链中执行：

1. `CompileDllCommand.CompileDllActiveBuildTarget()`
2. `BuildHelper.CopyPatchedAOTDllToCollectPath(PatchedAOTDLLCollectPath)` —源：`SettingsUtil` 中 strip 输出；列表来自 `HybridCLRSettings.patchAOTAssemblies`
3. `BuildHelper.CopyHotUpdateDllToCollectPath(HotUpdateDLLCollectPath)`

因此：**脚本包构建依赖 HybridCLR 工程设置中 AOT/热更程序集配置正确**，且 Collector 路径与 Settings 中文件夹一致。

---

## 5. 推荐工作流（简表）

| 场景 | 建议操作 |
|------|----------|
| 新工程首次接入 / 大版本主包 | HybridCLR Installer；`BuildApplication` 或 `打APK包`；确认 `patchAOTAssemblies`、桥接生成；再根据需要打热更。 |
| 仅热更资源 | `Hybrid Builder` → **BuildAsset**。 |
| 仅热更代码（AOT 未变） | 先 **验证元数据是否需要补充**；通过后 **BuildScript**（或 BuildAll）。 |
| AOT/主包已变 | 重新 **BuildApplication**（或菜单打 APK），更新 strip 与 `patchAOTAssemblies`，再发脚本/资源热更。 |
| 预制体大量引用 UnityEngine/TMPro | 发主包前执行 **补全热更新预制体依赖**，检查 `Assets/link.xml`。 |

---

## 6. 注意事项摘要

1. **Android**：`BuildApplication` 与 `BuildHelper.BuildAPK` 当前只对 `BuildTarget.Android` 返回成功逻辑；其他平台需自行扩展。  
2. **双 Package**：YooAsset Collector 至少两个 Package，否则 Hybrid Builder 视图无法初始化。  
3. **Script Package 与 Collector**：`PatchedAOTDLL` / `HotUpdateDLL` 文件夹必须在 **脚本包** 的 Group Collector 中注册，且 GUID 匹配，否则 `CheckScriptPathExsist` 失败。  
4. **版本号**：`BuildFinish` 会修改 `HybridBuilderSettings` 与 `HybridRuntimeSettings` 的版本字段并写盘；发版前确认 **自增版本** 开关与版本语义符合发布规范。  
5. **官方 HybridCLR 菜单**：如 `Generate/All`、`Generate/AOTDlls` 等与 `BuildHelper` 内部分步骤等价或互补，疑难问题可对照官方文档与控制台日志排查。

---

*文档生成依据：`Assets/Editor` 下 `HybridBuilderWindow.cs`、`HybridBuilderSettings.cs`、`HybridBuildPipeViewerBase.cs`、`HybridScriptableBuildPipelineViewer.cs`、`HybrdiScriptableBuildPipeline.cs`、`BuildPipelineTask/TaskBuildScript_SBP.cs`、`BuildHelper.cs` 及 `HybridRuntimeSettings.cs`。*
