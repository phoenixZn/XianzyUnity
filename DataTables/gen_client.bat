@echo off
setlocal
set WORKSPACE=%~dp0..
set LUBAN_DLL=%WORKSPACE%\Tools\Luban\Luban.dll
set CONF_ROOT=%~dp0

dotnet "%LUBAN_DLL%" ^
    --conf "%CONF_ROOT%luban.conf" ^
    -t client ^
    -c cs-simple-json ^
    -d json ^
    -x outputCodeDir="%WORKSPACE%\Assets\HotScripts\Product\Content\Gen\Luban" ^
    -x outputDataDir="%WORKSPACE%\Assets\HotAssets\Config\Luban"

exit /b %ERRORLEVEL%
