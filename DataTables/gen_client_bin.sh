#!/bin/bash
# 日后切二进制时使用：与 gen_client.sh 不要同时写到同一套 outputCodeDir。
# 运行时 ConfigManager 将 JSON.Parse 改为 new ByteBuf(rawBytes)。
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORKSPACE="$(cd "$SCRIPT_DIR/.." && pwd)"
LUBAN_DLL="$WORKSPACE/Tools/Luban/Luban.dll"
CONF_ROOT="$SCRIPT_DIR"

dotnet "$LUBAN_DLL" \
    --conf "$CONF_ROOT/luban.conf" \
    -t client \
    -c cs-bin \
    -d bin \
    -x outputCodeDir="$WORKSPACE/Assets/HotScripts/Product/Content/Gen/Luban" \
    -x outputDataDir="$WORKSPACE/Assets/HotAssets/Config/Luban"
