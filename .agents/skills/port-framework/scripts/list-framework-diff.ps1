# 只读分类游戏库相对核心库 HEAD 的改动。
# 允许的 git 子命令：rev-parse、status、diff、remote、merge-base、cat-file。
# 禁止在本脚本中 add、commit、restore、clean、reset、push、fetch。

[CmdletBinding()]
param(
    [string]$CoreRepo = 'E:\WorkGitHubZn\XianzyUnity',
    [string]$GameRepo = 'E:\KaboomZnWork\ka0_client\ka02_trunk\main\XDefense\td_trunk'
)

$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)

$Roots = @(
    'Assets/HotScripts/Framework',
    'Assets/AOTScripts'
)

function Write-ErrorLine([string]$Message) {
    Write-Output "ERROR $Message"
}

function Test-GitRepo([string]$Repo, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Repo)) {
        Write-ErrorLine "$Label missing: $Repo"
        return $false
    }
    & git -C $Repo rev-parse --is-inside-work-tree 1>$null 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-ErrorLine "$Label is not a git work tree: $Repo"
        return $false
    }
    return $true
}

function Get-GitLines([string]$Repo, [string[]]$GitArgs) {
    $output = & git -C $Repo @GitArgs
    $script:GitExit = $LASTEXITCODE
    if ($null -eq $output) {
        return @()
    }
    return @($output)
}

function Test-InScope([string]$Path) {
    $normalized = ($Path -replace '\\', '/').Trim()
    foreach ($root in $Roots) {
        if ($normalized.Equals($root, [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
        if ($normalized.StartsWith($root + '/', [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function ConvertFrom-GitPath([string]$Path) {
    $text = $Path.Trim()
    if (-not ($text.Length -ge 2 -and $text.StartsWith('"') -and $text.EndsWith('"'))) {
        return $text
    }
    $inner = $text.Substring(1, $text.Length - 2)
    $builder = New-Object System.Text.StringBuilder
    for ($i = 0; $i -lt $inner.Length; $i++) {
        if ($inner[$i] -eq '\' -and ($i + 1) -lt $inner.Length) {
            $next = $inner[$i + 1]
            if ($next -eq '\' -or $next -eq '"') {
                [void]$builder.Append($next)
                $i++
                continue
            }
        }
        [void]$builder.Append($inner[$i])
    }
    return $builder.ToString()
}

function Get-StatusPath([string]$Line) {
    if ($Line.Length -lt 4) {
        return $null
    }
    $rest = $Line.Substring(3)
    $arrow = ' -> '
    $arrowAt = $rest.LastIndexOf($arrow)
    if ($arrowAt -ge 0) {
        return ConvertFrom-GitPath $rest.Substring($arrowAt + $arrow.Length)
    }
    return ConvertFrom-GitPath $rest
}

if (-not (Test-GitRepo $CoreRepo 'core') -or -not (Test-GitRepo $GameRepo 'game')) {
    exit 1
}

$coreFull = [IO.Path]::GetFullPath($CoreRepo).TrimEnd('\')
$gameFull = [IO.Path]::GetFullPath($GameRepo).TrimEnd('\')
if ($coreFull.Equals($gameFull, [StringComparison]::OrdinalIgnoreCase)) {
    Write-ErrorLine 'core and game are the same path'
    exit 1
}

$coreHead = (& git -C $CoreRepo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($coreHead)) {
    Write-ErrorLine 'cannot resolve core HEAD'
    exit 1
}

$gameHead = (& git -C $GameRepo rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($gameHead)) {
    Write-ErrorLine 'cannot resolve game HEAD'
    exit 1
}

& git -C $GameRepo cat-file -e "$coreHead^{commit}" 2>$null
$coreObjectExists = ($LASTEXITCODE -eq 0)
if (-not $coreObjectExists) {
    Write-ErrorLine "core HEAD $coreHead is missing in the game repo; run git fetch upstream in the game repo and list again"
    exit 1
}

& git -C $GameRepo merge-base --is-ancestor $coreHead HEAD 2>$null
$coreIsAncestor = ($LASTEXITCODE -eq 0)

$upstreamHead = 'MISSING'
& git -C $GameRepo rev-parse --verify --quiet upstream/master 1>$null 2>$null
if ($LASTEXITCODE -eq 0) {
    $upstreamHead = (& git -C $GameRepo rev-parse upstream/master).Trim()
}

$upstreamUrl = (& git -C $GameRepo remote get-url upstream 2>$null)
if ($LASTEXITCODE -ne 0) {
    $upstreamUrl = 'MISSING'
}
$upstreamPush = (& git -C $GameRepo remote get-url --push upstream 2>$null)
if ($LASTEXITCODE -ne 0) {
    $upstreamPush = 'MISSING'
}

$coreStatus = Get-GitLines $CoreRepo @('-c', 'core.quotepath=false', 'status', '--porcelain=v1', '-uall')
if ($script:GitExit -ne 0) {
    Write-ErrorLine 'core status failed'
    exit 1
}
$coreDirty = @($coreStatus | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }).Count -gt 0

$diffArgs = @('-c', 'core.quotepath=false', 'diff', '--name-status', '-M', $coreHead, '--') + $Roots
$diffLines = Get-GitLines $GameRepo $diffArgs
if ($script:GitExit -ne 0) {
    Write-ErrorLine 'game diff against core HEAD failed'
    exit 1
}

$inScope = New-Object System.Collections.Generic.List[string]
foreach ($line in $diffLines) {
    if ([string]::IsNullOrWhiteSpace($line)) {
        continue
    }
    $parts = $line -split "`t"
    if ($parts.Length -lt 2) {
        continue
    }
    $status = $parts[0]
    if ($status.StartsWith('R') -or $status.StartsWith('C')) {
        $oldPath = ConvertFrom-GitPath $parts[1]
        $newPath = ConvertFrom-GitPath $parts[$parts.Length - 1]
        $inScope.Add("R`t$oldPath -> $newPath")
    }
    else {
        $path = ConvertFrom-GitPath $parts[1]
        $inScope.Add("$status`t$path")
    }
}

$gameStatus = Get-GitLines $GameRepo @('-c', 'core.quotepath=false', 'status', '--porcelain=v1', '-uall')
if ($script:GitExit -ne 0) {
    Write-ErrorLine 'game status failed'
    exit 1
}

$untracked = New-Object System.Collections.Generic.List[string]
$outOfScope = New-Object System.Collections.Generic.List[string]
foreach ($line in $gameStatus) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
        continue
    }
    $path = Get-StatusPath $line
    if ([string]::IsNullOrWhiteSpace($path)) {
        continue
    }
    if (Test-InScope $path) {
        if ($line.StartsWith('??')) {
            $untracked.Add("??`t$path")
        }
        continue
    }
    $outOfScope.Add($line)
}

$binary = New-Object System.Collections.Generic.List[string]
$numstatArgs = @('-c', 'core.quotepath=false', 'diff', '--numstat', $coreHead, '--') + $Roots
$numstat = Get-GitLines $GameRepo $numstatArgs
if ($script:GitExit -ne 0) {
    Write-ErrorLine 'game numstat failed'
    exit 1
}
foreach ($line in $numstat) {
    $parts = $line -split "`t", 3
    if ($parts.Length -ge 3 -and $parts[0] -eq '-' -and $parts[1] -eq '-') {
        $binary.Add((ConvertFrom-GitPath $parts[2]))
    }
}

$statArgs = @('-c', 'core.quotepath=false', 'diff', '--stat', $coreHead, '--') + $Roots
$stat = Get-GitLines $GameRepo $statArgs
if ($script:GitExit -ne 0) {
    Write-ErrorLine 'game diff stat failed'
    exit 1
}

$upstreamMatches = ($upstreamHead -eq $coreHead)
Write-Output "CORE_REPO=$CoreRepo"
Write-Output "CORE_HEAD=$coreHead"
Write-Output ("CORE_DIRTY=" + $(if ($coreDirty) { 'yes' } else { 'no' }))
Write-Output ("CORE_IS_ANCESTOR=" + $(if ($coreIsAncestor) { 'yes' } else { 'no' }))
Write-Output "GAME_REPO=$GameRepo"
Write-Output "GAME_HEAD=$gameHead"
Write-Output "UPSTREAM_HEAD=$upstreamHead"
Write-Output ("UPSTREAM_MATCHES_CORE=" + $(if ($upstreamMatches) { 'yes' } else { 'no' }))
Write-Output "UPSTREAM_URL=$upstreamUrl"
Write-Output "UPSTREAM_PUSH=$upstreamPush"
Write-Output 'ROOTS=Assets/HotScripts/Framework,Assets/AOTScripts'

Write-Output 'IN_SCOPE'
$inScope | Sort-Object | ForEach-Object { Write-Output $_ }

Write-Output 'UNTRACKED_IN_SCOPE'
$untracked | Sort-Object | ForEach-Object { Write-Output $_ }

Write-Output 'BINARY_IN_SCOPE'
$binary | Sort-Object | ForEach-Object { Write-Output $_ }

Write-Output 'OUT_OF_SCOPE'
$outOfScope | Sort-Object | ForEach-Object { Write-Output $_ }

Write-Output 'STAT'
$stat | ForEach-Object { Write-Output $_ }

if ($coreDirty) {
    Write-Output 'CORE_STATUS'
    $coreStatus | ForEach-Object { Write-Output $_ }
}

$count = $inScope.Count + $untracked.Count
Write-Output "IN_SCOPE_COUNT=$count"
if ($count -eq 0) {
    exit 3
}
exit 0
