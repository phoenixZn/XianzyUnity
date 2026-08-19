PureGameEnv — Unity 托管引用（refs）

从本机 Unity Editor 安装目录拷贝以下文件到本文件夹（与 ProjectSettings/ProjectVersion.txt 一致，当前工程为 2022.3.62f3）：

  <UnityEditor>\Editor\Data\Managed\UnityEngine\UnityEngine.CoreModule.dll
  <UnityEditor>\Editor\Data\Managed\UnityEngine\UnityEngine.SharedInternalsModule.dll
  <UnityEditor>\Editor\Data\Managed\UnityEngine\UnityEngine.dll

典型 Hub 路径示例（请按本机安装调整；版本与工程 ProjectVersion.txt 一致为佳）：

  C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\Managed\UnityEngine\
  D:\Program Files\Unity 2022.3.62f3\Editor\Data\Managed\UnityEngine\   （本机当前安装）
  D:\Unity\Hub\Editor\2022.3.58f1\Editor\Data\Managed\UnityEngine\   （本机曾用于验证）

若 dotnet build 仍报缺类型，将报错中提到的 UnityEngine.*Module.dll 从同目录继续拷贝到 refs，并在 PureGameEnv.csproj 中追加 <Reference>。

PureGameEnv 使用 net5.0（若本机仅有 .NET SDK 5）。安装 .NET 6+ 后可将 PureGameEnv.csproj 中 TargetFramework 改为 net6.0。

工程内 shim（不向 Assets 拷贝）：src/shim/AsyncAssetViewWrapper.Shim.cs；并 Compile Remove Product/GEnv.Ex.cs。IMGUI / UniTask / SysDebugProfiler 走 Assets 内 #if CONSOLE_CLIENT。详见仓库 cursor_doc/PureGameEnv-构建说明.md。

PowerShell 示例（路径存在时）：

  $u = "C:\Program Files\Unity\Hub\Editor\2022.3.62f3\Editor\Data\Managed\UnityEngine"
  Copy-Item "$u\UnityEngine.CoreModule.dll","$u\UnityEngine.SharedInternalsModule.dll","$u\UnityEngine.dll" -Destination .
