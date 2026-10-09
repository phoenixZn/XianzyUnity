#!/bin/bash
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
WORKSPACE="$(cd "$SCRIPT_DIR/.." && pwd)"
LUBAN_DLL="$WORKSPACE/Tools/Luban/Luban.dll"
CONF_ROOT="$SCRIPT_DIR"

dotnet "$LUBAN_DLL" \
    --conf "$CONF_ROOT/luban.conf" \
    -t client \
    -c cs-simple-json \
    -d json \
    -x outputCodeDir="$WORKSPACE/Assets/HotScripts/Product/Content/Gen/Luban" \
    -x outputDataDir="$WORKSPACE/Assets/HotAssets/Config/Luban"
