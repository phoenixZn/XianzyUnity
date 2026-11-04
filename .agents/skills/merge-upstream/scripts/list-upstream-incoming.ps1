# 只读列出 master 尚未包含的 upstream/master 提交与文件。
# 游戏库是 -GameRepo，省略时用当前目录的仓库根。核心库只读取该仓库 upstream 的地址，不打开其工作区。
# 允许的 git 子命令：rev-parse、status、diff、remote、merge-base、log。
# 禁止在本脚本中 fetch、merge、add、commit、checkout、reset、rebase、push。

[CmdletBinding()]
param(
    [string]$GameRepo
)

$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)

function Stop-List([string]$Message) {
    Write-Output "ERROR $Message"
    exit 1
}

function Get-GitLines([string]$Repo, [string[]]$GitArgs) {
    $output = & git --no-optional-locks -C $Repo @GitArgs 2>$null
    $script:GitExit = $LASTEXITCODE
    if ($null -eq $output) {
        return @()
    }
    return @($output)
}

function Get-OneLine([string]$Repo, [string[]]$GitArgs) {
    $lines = @(Get-GitLines $Repo $GitArgs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($lines.Count -eq 0) {
        return ''
    }
    return (($lines -join "`n").Trim())
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

function ConvertTo-RepoPath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return ''
    }
    return (($Path -replace '\\', '/').Trim())
}

function New-PathSet {
    $set = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList ([StringComparer]::OrdinalIgnoreCase)
    # HashSet 可枚举。逗号让函数返回集合本身，而不是把它的成员摊开成空输出。
    return ,$set
}

if ([string]::IsNullOrWhiteSpace($GameRepo)) {
    $discovered = & git --no-optional-locks rev-parse --show-toplevel 2>$null
    $script:GitExit = $LASTEXITCODE
    if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($discovered)) {
        Stop-List 'cannot resolve game repo; pass -GameRepo or run inside the game repo'
    }
    $GameRepo = ([string]$discovered).Trim()
}

if (-not (Test-Path -LiteralPath $GameRepo)) {
    Stop-List "game repo missing: $GameRepo"
}

$requestedRepo = $GameRepo
$GameRepo = Get-OneLine $requestedRepo @('rev-parse', '--show-toplevel')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($GameRepo)) {
    Stop-List "game repo is not a git work tree: $requestedRepo"
}

$gitDir = Get-OneLine $GameRepo @('rev-parse', '--absolute-git-dir')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($gitDir)) {
    Stop-List 'cannot resolve git dir'
}
foreach ($name in @('MERGE_HEAD', 'rebase-merge', 'rebase-apply')) {
    if (Test-Path -LiteralPath (Join-Path $gitDir $name)) {
        Stop-List 'merge or rebase already in progress'
    }
}

$coreUrl = Get-OneLine $GameRepo @('remote', 'get-url', 'upstream')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($coreUrl)) {
    Stop-List 'upstream remote is missing'
}
$upstreamPush = Get-OneLine $GameRepo @('remote', 'get-url', '--push', 'upstream')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($upstreamPush)) {
    Stop-List 'cannot read upstream push url'
}
if (-not $upstreamPush.Equals('DISABLED', [StringComparison]::Ordinal)) {
    Stop-List "not a game repo: upstream push url is '$upstreamPush', expected DISABLED"
}

$head = Get-OneLine $GameRepo @('rev-parse', 'HEAD')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
    Stop-List 'cannot resolve HEAD'
}
$branch = Get-OneLine $GameRepo @('rev-parse', '--abbrev-ref', 'HEAD')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($branch)) {
    Stop-List 'cannot resolve current branch'
}

[void](Get-GitLines $GameRepo @('rev-parse', '--verify', '--quiet', 'refs/heads/master'))
if ($script:GitExit -ne 0) {
    Stop-List 'local branch master is missing'
}
$master = Get-OneLine $GameRepo @('rev-parse', 'refs/heads/master')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($master)) {
    Stop-List 'cannot resolve master'
}

[void](Get-GitLines $GameRepo @('rev-parse', '--verify', '--quiet', 'upstream/master'))
if ($script:GitExit -ne 0) {
    Stop-List 'upstream/master is missing; run git fetch upstream and list again'
}
$upstreamHead = Get-OneLine $GameRepo @('rev-parse', 'upstream/master')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($upstreamHead)) {
    Stop-List 'cannot resolve upstream/master'
}

$mergeBase = Get-OneLine $GameRepo @('merge-base', 'master', 'upstream/master')
if ($script:GitExit -ne 0 -or [string]::IsNullOrWhiteSpace($mergeBase)) {
    Stop-List 'master and upstream/master have no merge base'
}

$logArgs = @('-c', 'core.quotepath=false', '-c', 'i18n.logOutputEncoding=utf-8')
$incomingLines = @(Get-GitLines $GameRepo ($logArgs + @('log', '--oneline', 'master..upstream/master')) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($script:GitExit -ne 0) {
    Stop-List 'incoming log failed'
}
$incomingHashes = @(Get-GitLines $GameRepo @('log', '--format=%H', 'master..upstream/master') | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($script:GitExit -ne 0) {
    Stop-List 'incoming hashes failed'
}
if ($incomingLines.Count -ne $incomingHashes.Count) {
    Stop-List 'incoming log and hashes differ'
}
$localOnly = @(Get-GitLines $GameRepo ($logArgs + @('log', '--oneline', 'upstream/master..master')) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($script:GitExit -ne 0) {
    Stop-List 'local-only log failed'
}

$diffArgs = @('-c', 'core.quotepath=false')
$nameStatus = @(Get-GitLines $GameRepo ($diffArgs + @('diff', '--name-status', '-M', 'master...upstream/master')) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($script:GitExit -ne 0) {
    Stop-List 'name-status failed'
}
$stat = @(Get-GitLines $GameRepo ($diffArgs + @('diff', '--stat', 'master...upstream/master')) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
if ($script:GitExit -ne 0) {
    Stop-List 'diff stat failed'
}

$statusLines = @(Get-GitLines $GameRepo ($diffArgs + @('status', '--porcelain=v1', '-uall')))
if ($script:GitExit -ne 0) {
    Stop-List 'status failed'
}
foreach ($line in $statusLines) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 2) {
        continue
    }
    if ($line[0] -eq 'U' -or $line[1] -eq 'U') {
        Stop-List 'unmerged paths already present'
    }
}

$incomingSet = New-PathSet
foreach ($line in $nameStatus) {
    $parts = $line -split "`t"
    if ($parts.Length -lt 2) {
        continue
    }
    $status = $parts[0]
    $paths = @()
    if ($status.StartsWith('R') -or $status.StartsWith('C')) {
        $paths = @($parts[1], $parts[$parts.Length - 1])
    }
    else {
        $paths = @($parts[1])
    }
    foreach ($rawPath in $paths) {
        $repoPath = ConvertTo-RepoPath (ConvertFrom-GitPath $rawPath)
        if (-not [string]::IsNullOrWhiteSpace($repoPath)) {
            [void]$incomingSet.Add($repoPath)
        }
    }
}

$dirtyLines = New-Object System.Collections.Generic.List[string]
$dirtyPaths = New-Object System.Collections.Generic.List[string]
foreach ($line in $statusLines) {
    if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
        continue
    }
    $dirtyLines.Add($line)
    $path = ConvertTo-RepoPath (Get-StatusPath $line)
    if (-not [string]::IsNullOrWhiteSpace($path)) {
        $dirtyPaths.Add($path)
    }
    $rest = $line.Substring(3)
    $arrow = ' -> '
    $arrowAt = $rest.LastIndexOf($arrow)
    if ($arrowAt -gt 0) {
        $oldPath = ConvertTo-RepoPath (ConvertFrom-GitPath $rest.Substring(0, $arrowAt))
        if (-not [string]::IsNullOrWhiteSpace($oldPath)) {
            $dirtyPaths.Add($oldPath)
        }
    }
}

$overlapPaths = New-Object System.Collections.Generic.List[string]
$overlapSeen = New-PathSet
foreach ($path in $dirtyPaths) {
    if ($incomingSet.Contains($path) -and $overlapSeen.Add($path)) {
        $overlapPaths.Add($path)
    }
}

$onMaster = $branch.Equals('master', [StringComparison]::Ordinal)
$checkout = 'ok'
$checkoutBlocked = New-Object System.Collections.Generic.List[string]
if ($onMaster) {
    $checkout = 'already'
}
else {
    # 预测 checkout master 会不会被本地改动挡住，这里不切换分支。
    $headToMaster = @(Get-GitLines $GameRepo ($diffArgs + @('diff', '--no-renames', '--name-only', 'HEAD', 'master')) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($script:GitExit -ne 0) {
        Stop-List 'diff HEAD master failed'
    }
    $addedOnMaster = @(Get-GitLines $GameRepo ($diffArgs + @('diff', '--no-renames', '--name-only', '--diff-filter=A', 'HEAD', 'master')) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($script:GitExit -ne 0) {
        Stop-List 'diff added-on-master failed'
    }
    $headToMasterSet = New-PathSet
    foreach ($path in $headToMaster) {
        [void]$headToMasterSet.Add((ConvertTo-RepoPath $path))
    }
    $addedSet = New-PathSet
    foreach ($path in $addedOnMaster) {
        [void]$addedSet.Add((ConvertTo-RepoPath $path))
    }
    $blockedSeen = New-PathSet
    foreach ($line in $statusLines) {
        if ([string]::IsNullOrWhiteSpace($line) -or $line.Length -lt 4) {
            continue
        }
        $path = ConvertTo-RepoPath (Get-StatusPath $line)
        if ([string]::IsNullOrWhiteSpace($path)) {
            continue
        }
        $hit = $false
        if ($line.StartsWith('??')) {
            $hit = $addedSet.Contains($path)
        }
        else {
            $hit = $headToMasterSet.Contains($path)
        }
        if ($hit -and $blockedSeen.Add($path)) {
            $checkoutBlocked.Add($path)
        }
    }
    if ($checkoutBlocked.Count -gt 0) {
        $checkout = 'blocked'
    }
}

Write-Output 'ROLE=game'
Write-Output "GAME_REPO=$GameRepo"
Write-Output "CORE_URL=$coreUrl"
Write-Output "UPSTREAM_PUSH=$upstreamPush"
Write-Output "BRANCH=$branch"
Write-Output ("ON_MASTER=" + $(if ($onMaster) { 'yes' } else { 'no' }))
Write-Output "HEAD=$head"
Write-Output "MASTER=$master"
Write-Output "UPSTREAM_HEAD=$upstreamHead"
Write-Output "MERGE_BASE=$mergeBase"
Write-Output "CHECKOUT_MASTER=$checkout"
Write-Output ("OVERLAP=" + $(if ($overlapPaths.Count -gt 0) { 'yes' } else { 'no' }))
Write-Output "INCOMING_COUNT=$($incomingHashes.Count)"
Write-Output 'INCOMING_RANGE=master..upstream/master'
Write-Output 'INCOMING'
$incomingLines | ForEach-Object { Write-Output $_ }
Write-Output 'INCOMING_HASHES'
$incomingHashes | ForEach-Object { Write-Output $_ }
Write-Output 'LOCAL_ONLY'
$localOnly | ForEach-Object { Write-Output $_ }
Write-Output 'NAME_STATUS'
$nameStatus | ForEach-Object { Write-Output $_ }
Write-Output 'STAT'
$stat | ForEach-Object { Write-Output $_ }
Write-Output 'DIRTY'
$dirtyLines | ForEach-Object { Write-Output $_ }
Write-Output 'OVERLAP_PATHS'
$overlapPaths | ForEach-Object { Write-Output $_ }
Write-Output 'CHECKOUT_BLOCKED'
$checkoutBlocked | ForEach-Object { Write-Output $_ }

if ($incomingHashes.Count -eq 0) {
    exit 3
}
exit 0
