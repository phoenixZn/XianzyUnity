#Requires -Version 5.1
<#
.SYNOPSIS
    扫描或清除目录内 PNG 的 iTXt 信息块。

.DESCRIPTION
    像素块原样保留，只处理 iTXt。默认包含子目录。

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\PngITxt.ps1 scan "Assets\Content\UI"

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\PngITxt.ps1 strip "Assets\Content\UI"

.EXAMPLE
    powershell -NoProfile -ExecutionPolicy Bypass -File .\PngITxt.ps1 scan "Assets\Content\UI" -NoRecurse
#>
[CmdletBinding()]
param(
    [Parameter(Position = 0, Mandatory = $true)]
    [ValidateSet('scan', 'strip')]
    [string]$Command,

    [Parameter(Position = 1, Mandatory = $true)]
    [string]$Path,

    [switch]$NoRecurse
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8

function Read-BeU32 {
    param(
        [byte[]]$Bytes,
        [int]$Offset
    )

    $tmp = New-Object byte[] 4
    [Array]::Copy($Bytes, $Offset, $tmp, 0, 4)
    [Array]::Reverse($tmp)
    return [BitConverter]::ToUInt32($tmp, 0)
}

function Test-PngSignature {
    param([byte[]]$Bytes)

    if ($Bytes.Length -lt 8) {
        return $false
    }

    return $Bytes[0] -eq 137 -and $Bytes[1] -eq 80 -and $Bytes[2] -eq 78 -and $Bytes[3] -eq 71 `
        -and $Bytes[4] -eq 13 -and $Bytes[5] -eq 10 -and $Bytes[6] -eq 26 -and $Bytes[7] -eq 10
}

function Read-PngITxt {
    param(
        [byte[]]$Bytes,
        [switch]$BuildStripped
    )

    $info = [ordered]@{
        Ok         = $false
        Error      = $null
        ItxtBytes  = [int64]0
        ItxtCount  = 0
        Stripped   = $null
    }

    if (-not (Test-PngSignature $Bytes)) {
        $info.Error = '不是 PNG'
        return [pscustomobject]$info
    }

    $output = $null
    if ($BuildStripped) {
        $output = New-Object System.IO.MemoryStream
        $output.Write($Bytes, 0, 8)
    }

    $pos = [int64]8
    $sawEnd = $false
    while (($pos + 8) -le $Bytes.Length) {
        if ($pos -gt [int]::MaxValue) {
            $info.Error = '文件过大'
            return [pscustomobject]$info
        }

        $len = [int64](Read-BeU32 -Bytes $Bytes -Offset ([int]$pos))
        $type = [Text.Encoding]::ASCII.GetString($Bytes, ([int]$pos + 4), 4)
        $chunkSize = [int64]12 + $len
        if ($len -lt 0 -or ($pos + $chunkSize) -gt $Bytes.Length -or $chunkSize -gt [int]::MaxValue) {
            $info.Error = "块 $type 长度非法"
            return [pscustomobject]$info
        }

        $isText = $type -eq 'iTXt'
        if ($isText) {
            $info.ItxtCount = [int]$info.ItxtCount + 1
            $info.ItxtBytes = [int64]$info.ItxtBytes + $chunkSize
        }
        elseif ($null -ne $output) {
            $output.Write($Bytes, [int]$pos, [int]$chunkSize)
        }

        $pos += $chunkSize
        if ($type -eq 'IEND') {
            $sawEnd = $true
            break
        }
    }

    if (-not $sawEnd) {
        $info.Error = '缺少 IEND'
        return [pscustomobject]$info
    }

    if ($pos -ne $Bytes.Length) {
        $info.Error = 'IEND 之后仍有数据'
        return [pscustomobject]$info
    }

    $info.Ok = $true
    if ($null -ne $output -and $info.ItxtCount -gt 0) {
        $info.Stripped = $output.ToArray()
    }

    return [pscustomobject]$info
}

function Get-RelativeDisplayPath {
    param(
        [string]$BaseDirectory,
        [string]$FullPath
    )

    $base = [Uri]($BaseDirectory.TrimEnd('\') + '\')
    $full = [Uri]$FullPath
    $relative = [Uri]::UnescapeDataString($base.MakeRelativeUri($full).ToString())
    return $relative.Replace('/', '\')
}

function Get-TargetPngFiles {
    param(
        [string]$TargetPath,
        [switch]$TopOnly
    )

    if (-not (Test-Path -LiteralPath $TargetPath)) {
        throw "路径不存在: $TargetPath"
    }

    $item = Get-Item -LiteralPath $TargetPath
    if (-not $item.PSIsContainer) {
        if ($item.Extension -ne '.png') {
            throw "不是 PNG 文件: $TargetPath"
        }

        return @($item)
    }

    if ($TopOnly) {
        return @(Get-ChildItem -LiteralPath $item.FullName -Filter *.png -File)
    }

    return @(Get-ChildItem -LiteralPath $item.FullName -Filter *.png -File -Recurse)
}

function Write-StripFile {
    param(
        [string]$FilePath,
        [byte[]]$Bytes
    )

    $tmp = $FilePath + '.itxt-strip.tmp'
    try {
        [System.IO.File]::WriteAllBytes($tmp, $Bytes)
        [System.IO.File]::Copy($tmp, $FilePath, $true)
    }
    finally {
        if (Test-Path -LiteralPath $tmp) {
            Remove-Item -LiteralPath $tmp -Force
        }
    }
}

if (-not (Test-Path -LiteralPath $Path)) {
    Write-Output "路径不存在: $Path"
    exit 2
}

$resolved = (Resolve-Path -LiteralPath $Path).Path
$files = @(Get-TargetPngFiles -TargetPath $resolved -TopOnly:$NoRecurse)
$baseDir = $resolved
if (-not (Get-Item -LiteralPath $resolved).PSIsContainer) {
    $baseDir = Split-Path -Parent $resolved
}

$scanned = 0
$withText = 0
$plain = 0
$textBytes = [int64]0
$changed = 0
$removedBytes = [int64]0
$failed = 0
$hits = New-Object System.Collections.Generic.List[object]

foreach ($file in $files) {
    $scanned++
    $display = Get-RelativeDisplayPath -BaseDirectory $baseDir -FullPath $file.FullName
    try {
        $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
        $info = Read-PngITxt -Bytes $bytes -BuildStripped:($Command -eq 'strip')
    }
    catch {
        $failed++
        Write-Output ("失败  {0}  {1}" -f $display, $_.Exception.Message)
        continue
    }

    if (-not $info.Ok) {
        $failed++
        Write-Output ("失败  {0}  {1}" -f $display, $info.Error)
        continue
    }

    if ($info.ItxtCount -le 0) {
        $plain++
        continue
    }

    $withText++
    $textBytes += [int64]$info.ItxtBytes

    if ($Command -eq 'scan') {
        $hits.Add([pscustomobject]@{
            ItxtBytes = [int64]$info.ItxtBytes
            FileBytes = [int64]$file.Length
            Count     = [int]$info.ItxtCount
            Path      = $display
        })
        continue
    }

    if ($file.IsReadOnly) {
        $failed++
        Write-Output ("只读  {0}  含 {1} 字节 iTXt，未修改" -f $display, $info.ItxtBytes)
        continue
    }

    try {
        Write-StripFile -FilePath $file.FullName -Bytes $info.Stripped
    }
    catch {
        $failed++
        Write-Output ("失败  {0}  {1}" -f $display, $_.Exception.Message)
        continue
    }

    $changed++
    $removedBytes += [int64]$info.ItxtBytes
    $after = [int64]$file.Length - [int64]$info.ItxtBytes
    Write-Output ("清除  {0}  {1} -> {2}  去掉 {3} 字节" -f $display, $file.Length, $after, $info.ItxtBytes)
}

if ($Command -eq 'scan') {
    foreach ($hit in ($hits | Sort-Object -Property ItxtBytes -Descending)) {
        Write-Output ("iTXt  {0,12} 字节  文件 {1,12} 字节  块数 {2}  {3}" -f $hit.ItxtBytes, $hit.FileBytes, $hit.Count, $hit.Path)
    }

    Write-Output ("扫描 {0} 个 PNG，其中 {1} 个含 iTXt，合计 {2} 字节。失败 {3} 个。" -f $scanned, $withText, $textBytes, $failed)
}
else {
    Write-Output ("扫描 {0} 个 PNG，清除 {1} 个，去掉 {2} 字节。无 iTXt {3} 个。失败 {4} 个。" -f $scanned, $changed, $removedBytes, $plain, $failed)
}

if ($failed -gt 0) {
    exit 1
}

exit 0
