@echo off
REM 日后切二进制时使用：与 gen_client.bat 不要同时写到同一套 outputCodeDir。
REM 运行时 ConfigManager 将 JSON.Parse 改为 new ByteBuf(rawBytes)。
setlocal
set WORKSPACE=%~dp0..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0

dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%luban.conf" ^
    -t client ^
    -c cs-bin ^
    -d bin ^
    -x outputCodeDir="%WORKSPACE%\Assets\HotScripts\Product\Content\Gen\Luban" ^
    -x outputDataDir="%WORKSPACE%\Assets\HotAssets\Config\Luban"

exit /b %ERRORLEVEL%
