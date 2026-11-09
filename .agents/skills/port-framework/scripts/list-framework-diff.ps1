# 只读列举回写范围，生成绑定基线、暂存状态与文件字节的审核指纹。
# 仅调用 rev-parse、status、diff、remote、merge-base、cat-file；不 fetch 或写入 Git。

[CmdletBinding()]
param(
    [string]$CoreRepo,
    [string]$GameRepo,
    [string[]]$ExtraPath = @()
)

$ErrorActionPreference = 'Continue'
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)

function Stop-List([string]$Message) {
    Write-Output "ERROR $Message"
    exit 1
}

function Get-GitLines([string]$Repo, [string[]]$GitArgs) {
    $output = & git --no-optional-locks --literal-pathspecs -C $Repo @GitArgs 2>$null
    $script:GitExit = $LASTEXITCODE
    if ($null -eq $output) { return @() }
    return @($output)
}

function Get-RequiredLine([string]$Repo, [string[]]$GitArgs) {
    $lines = @(Get-GitLines $Repo $GitArgs | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($script:GitExit -ne 0 -or $lines.Count -ne 1) {
        Stop-List "cannot query '$($GitArgs -join ' ')' in $Repo"
    }
    return ([string]$lines[0]).Trim()
}

function Get-RepoRoot([string]$Repo) {
    if ([string]::IsNullOrWhiteSpace($Repo) -or -not (Test-Path -LiteralPath $Repo -PathType Container)) {
        Stop-List "missing repository: $Repo"
    }
    $root = Get-RequiredLine $Repo @('rev-parse', '--show-toplevel')
    return [IO.Path]::GetFullPath($root).TrimEnd([char[]]'\/')
}

function ConvertTo-RelativePath([string]$Path) {
    $normalized = $Path.Replace('\', '/').TrimEnd('/')
    $segments = $normalized.Split('/')
    if ([string]::IsNullOrWhiteSpace($normalized) -or [IO.Path]::IsPathRooted($normalized) -or
        $normalized -match '[:*?\x00-\x1f]' -or $segments -contains '..' -or
        $segments -contains '.' -or $segments -contains '' -or $segments -contains '.git') {
        Stop-List "unsafe relative path: $Path"
    }
    return $normalized
}

function ConvertFrom-GitPath([string]$Path) {
    $text = $Path
    if ($text.StartsWith('"')) {
        if (-not $text.EndsWith('"') -or $text.Length -lt 2) {
            Stop-List 'malformed quoted Git path'
        }
        $inner = $text.Substring(1, $text.Length - 2)
        $builder = New-Object System.Text.StringBuilder
        for ($i = 0; $i -lt $inner.Length; $i++) {
            if ($inner[$i] -eq '\') {
                $i++
                if ($i -ge $inner.Length -or ($inner[$i] -ne '\' -and $inner[$i] -ne '"')) {
                    Stop-List 'Git path contains unsupported control-character escapes'
                }
            }
            [void]$builder.Append($inner[$i])
        }
        $text = $builder.ToString()
    }
    return ConvertTo-RelativePath $text
}

function Test-InScope([string]$Path) {
    foreach ($root in $Roots) {
        if ($Path.Equals($root, [StringComparison]::OrdinalIgnoreCase) -or
            $Path.StartsWith($root + '/', [StringComparison]::OrdinalIgnoreCase)) {
            return $true
        }
    }
    return $false
}

function Get-Operation([string]$Repo) {
    $gitDir = Get-RequiredLine $Repo @('rev-parse', '--absolute-git-dir')
    foreach ($marker in @('MERGE_HEAD', 'rebase-merge', 'rebase-apply', 'CHERRY_PICK_HEAD', 'REVERT_HEAD', 'sequencer')) {
        if (Test-Path -LiteralPath (Join-Path $gitDir $marker)) { return $marker }
    }
    return 'none'
}

function Get-FileState([string]$Repo, [string]$Path) {
    # 逐层检查，避免通过目录链接读取仓库外的文件；缺失文件仍进入指纹。
    $full = [IO.Path]::GetFullPath((Join-Path $Repo $Path))
    if (-not $full.StartsWith($Repo + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        Stop-List "path escapes repository: $Path"
    }
    $cursor = $Repo
    foreach ($segment in $Path.Split('/')) {
        $cursor = Join-Path $cursor $segment
        $item = Get-Item -LiteralPath $cursor -Force -ErrorAction SilentlyContinue
        if ($null -ne $item -and ($item.Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            Stop-List "reparse point is not supported: $Path"
        }
    }
    if (-not (Test-Path -LiteralPath $full)) {
        return [ordered]@{ Path = $Path; State = 'missing'; SHA256 = '' }
    }
    if (-not (Test-Path -LiteralPath $full -PathType Leaf)) {
        Stop-List "selected path is not a regular file: $Path"
    }
    try { $hash = (Get-FileHash -LiteralPath $full -Algorithm SHA256 -ErrorAction Stop).Hash }
    catch { Stop-List "cannot hash file: $Path" }
    return [ordered]@{ Path = $Path; State = 'file'; SHA256 = $hash }
}

$CoreRepo = Get-RepoRoot $CoreRepo
$GameRepo = Get-RepoRoot $GameRepo
if ($CoreRepo.Equals($GameRepo, [StringComparison]::OrdinalIgnoreCase)) {
    Stop-List 'core and game are the same repository root'
}

$Roots = @('Assets/HotScripts/Framework', 'Assets/AOTScripts')
foreach ($extra in $ExtraPath) { $Roots += ConvertTo-RelativePath $extra }
$Roots = @($Roots | Sort-Object -Unique)

$coreHead = Get-RequiredLine $CoreRepo @('rev-parse', 'HEAD')
$gameHead = Get-RequiredLine $GameRepo @('rev-parse', 'HEAD')
$coreBranch = Get-RequiredLine $CoreRepo @('rev-parse', '--abbrev-ref', 'HEAD')
$gameBranch = Get-RequiredLine $GameRepo @('rev-parse', '--abbrev-ref', 'HEAD')
$coreOperation = Get-Operation $CoreRepo
$gameOperation = Get-Operation $GameRepo

$null = Get-GitLines $GameRepo @('cat-file', '-e', "$coreHead^{commit}")
if ($script:GitExit -ne 0) { Stop-List 'core HEAD is missing in game; fetch upstream and list again' }
$null = Get-GitLines $GameRepo @('merge-base', '--is-ancestor', $coreHead, $gameHead)
if ($script:GitExit -gt 1) { Stop-List 'cannot check core ancestry' }
$coreIsAncestor = $script:GitExit -eq 0

$upstreamHead = Get-RequiredLine $GameRepo @('rev-parse', '--verify', 'upstream/master')
$upstreamUrl = Get-RequiredLine $GameRepo @('remote', 'get-url', 'upstream')
$upstreamPush = @(Get-GitLines $GameRepo @('remote', 'get-url', '--push', '--all', 'upstream'))
if ($script:GitExit -ne 0 -or $upstreamPush.Count -eq 0) { Stop-List 'cannot read upstream push URLs' }

$statusArgs = @('-c', 'core.quotepath=false', 'status', '--porcelain=v1', '--no-renames', '-uall')
$coreStatus = @(Get-GitLines $CoreRepo $statusArgs)
if ($script:GitExit -ne 0) { Stop-List 'core status failed' }
$gameStatus = @(Get-GitLines $GameRepo $statusArgs)
if ($script:GitExit -ne 0) { Stop-List 'game status failed' }
$coreDirty = $coreStatus.Count -gt 0

# 不做重命名推断，确保跨范围移动时每个端点都按自己的字面路径审核。
$diffPrefix = @('-c', 'core.quotepath=false', 'diff', '--no-ext-diff', '--no-textconv', '--no-renames')
$diffLines = @(Get-GitLines $GameRepo ($diffPrefix + @('--name-status', $coreHead, '--') + $Roots))
if ($script:GitExit -ne 0) { Stop-List 'name-status failed' }
$indexState = @(Get-GitLines $GameRepo ($diffPrefix + @('--cached', '--raw', '--no-abbrev', '--') + $Roots))
if ($script:GitExit -ne 0) { Stop-List 'index snapshot failed' }
$rawDiff = @(Get-GitLines $GameRepo ($diffPrefix + @('--raw', '--no-abbrev', $coreHead, '--') + $Roots))
if ($script:GitExit -ne 0) { Stop-List 'raw diff failed' }

$inScope = New-Object System.Collections.Generic.List[string]
$selected = New-Object 'System.Collections.Generic.HashSet[string]' -ArgumentList ([StringComparer]::OrdinalIgnoreCase)
foreach ($line in $diffLines) {
    $parts = $line -split "`t", 2
    if ($parts.Count -ne 2) { Stop-List 'malformed name-status record' }
    $path = ConvertFrom-GitPath $parts[1]
    $inScope.Add("$($parts[0])`t$path")
    [void]$selected.Add($path)
}

$untracked = New-Object System.Collections.Generic.List[string]
$outOfScope = New-Object System.Collections.Generic.List[string]
$blockers = New-Object System.Collections.Generic.List[string]
foreach ($line in $gameStatus) {
    if ($line.Length -lt 4) { Stop-List 'malformed status record' }
    $path = ConvertFrom-GitPath $line.Substring(3)
    if (-not (Test-InScope $path)) { $outOfScope.Add($line); continue }
    if ($line.StartsWith('??')) {
        $untracked.Add($path)
        [void]$selected.Add($path)
        $targetState = Get-FileState $CoreRepo $path
        if ($targetState.State -ne 'missing') { $blockers.Add("UNTRACKED_TARGET_EXISTS: $path") }
    }
    if ($line[0] -eq 'U' -or $line[1] -eq 'U' -or $line.StartsWith('AA') -or $line.StartsWith('DD')) {
        $blockers.Add("UNMERGED: $path")
    }
}

$fileStates = @(
    foreach ($path in @($selected | Sort-Object)) {
        $null = Get-FileState $CoreRepo $path
        Get-FileState $GameRepo $path
    }
)

$binary = New-Object System.Collections.Generic.List[string]
$numstat = @(Get-GitLines $GameRepo ($diffPrefix + @('--numstat', $coreHead, '--') + $Roots))
if ($script:GitExit -ne 0) { Stop-List 'numstat failed' }
foreach ($line in $numstat) {
    $parts = $line -split "`t", 3
    if ($parts.Count -eq 3 -and $parts[0] -eq '-' -and $parts[1] -eq '-') {
        $binary.Add((ConvertFrom-GitPath $parts[2]))
    }
}
# 未跟踪文件不会出现在普通 diff 中，单独让 Git 判定二进制。
foreach ($path in $untracked) {
    $probe = @(Get-GitLines $GameRepo ($diffPrefix + @('--no-index', '--numstat', '--', '/dev/null', (Join-Path $GameRepo $path))))
    if ($script:GitExit -ne 0 -and $script:GitExit -ne 1) { Stop-List "binary probe failed: $path" }
    if (@($probe | Where-Object { $_ -match '^-\t-\t' }).Count -gt 0) { $binary.Add($path) }
}
$stat = @(Get-GitLines $GameRepo ($diffPrefix + @('--stat', $coreHead, '--') + $Roots))
if ($script:GitExit -ne 0) { Stop-List 'stat failed' }

if ($coreDirty) { $blockers.Add('CORE_DIRTY') }
if ($coreBranch -cne 'master') { $blockers.Add('CORE_NOT_ON_MASTER') }
if ($gameBranch -cne 'master') { $blockers.Add('GAME_NOT_ON_MASTER') }
if ($coreOperation -ne 'none') { $blockers.Add("CORE_OPERATION: $coreOperation") }
if ($gameOperation -ne 'none') { $blockers.Add("GAME_OPERATION: $gameOperation") }
if (-not $coreIsAncestor) { $blockers.Add('CORE_NOT_ANCESTOR') }
if ($upstreamHead -ne $coreHead) { $blockers.Add('UPSTREAM_DOES_NOT_MATCH_CORE') }
if ($upstreamPush.Count -ne 1 -or $upstreamPush[0] -cne 'DISABLED') { $blockers.Add('UPSTREAM_PUSH_NOT_DISABLED') }
foreach ($path in $binary) { $blockers.Add("BINARY: $path") }
if (@(($rawDiff + $indexState) | Where-Object { $_ -match '^:(120000|160000) ' -or $_ -match '^:\d{6} (120000|160000) ' }).Count -gt 0) {
    $blockers.Add('SYMLINK_OR_SUBMODULE_IN_SCOPE')
}

# 有序 JSON 避免文本拼接歧义；文件按原始字节哈希，捕捉同名文件的后续编辑。
$snapshot = [ordered]@{
    CoreRepo = $CoreRepo; GameRepo = $GameRepo
    CoreHead = $coreHead; GameHead = $gameHead
    CoreBranch = $coreBranch; GameBranch = $gameBranch
    CoreOperation = $coreOperation; GameOperation = $gameOperation
    UpstreamHead = $upstreamHead; UpstreamUrl = $upstreamUrl; UpstreamPush = @($upstreamPush)
    Roots = $Roots; CoreStatus = $coreStatus; GameStatus = $gameStatus
    Diff = $diffLines; Index = $indexState; RawDiff = $rawDiff; Files = $fileStates
    Blockers = @($blockers)
}
$sha = [Security.Cryptography.SHA256]::Create()
try {
    $bytes = [Text.Encoding]::UTF8.GetBytes(($snapshot | ConvertTo-Json -Depth 6 -Compress))
    $reviewHash = [BitConverter]::ToString($sha.ComputeHash($bytes)).Replace('-', '')
}
finally { $sha.Dispose() }

Write-Output "CORE_REPO=$CoreRepo"
Write-Output "CORE_HEAD=$coreHead"
Write-Output "CORE_BRANCH=$coreBranch"
Write-Output "CORE_OPERATION=$coreOperation"
Write-Output ("CORE_DIRTY=" + $(if ($coreDirty) { 'yes' } else { 'no' }))
Write-Output ("CORE_IS_ANCESTOR=" + $(if ($coreIsAncestor) { 'yes' } else { 'no' }))
Write-Output "GAME_REPO=$GameRepo"
Write-Output "GAME_HEAD=$gameHead"
Write-Output "GAME_BRANCH=$gameBranch"
Write-Output "GAME_OPERATION=$gameOperation"
Write-Output "UPSTREAM_HEAD=$upstreamHead"
Write-Output ("UPSTREAM_MATCHES_CORE=" + $(if ($upstreamHead -eq $coreHead) { 'yes' } else { 'no' }))
Write-Output "UPSTREAM_URL=$upstreamUrl"
Write-Output "UPSTREAM_PUSH=$($upstreamPush -join ',')"
Write-Output "ROOTS=$($Roots -join ',')"
Write-Output "REVIEW_SHA256=$reviewHash"
Write-Output ("BLOCKED=" + $(if ($blockers.Count -gt 0) { 'yes' } else { 'no' }))
Write-Output 'BLOCKERS'
$blockers | ForEach-Object { Write-Output $_ }
Write-Output 'IN_SCOPE'
$inScope | Sort-Object | ForEach-Object { Write-Output $_ }
Write-Output 'UNTRACKED_IN_SCOPE'
$untracked | Sort-Object | ForEach-Object { Write-Output "??`t$_" }
Write-Output 'BINARY_IN_SCOPE'
$binary | Sort-Object -Unique | ForEach-Object { Write-Output $_ }
Write-Output 'OUT_OF_SCOPE'
$outOfScope | Sort-Object | ForEach-Object { Write-Output $_ }
Write-Output 'FILE_STATES'
$fileStates | ForEach-Object { Write-Output ($_ | ConvertTo-Json -Compress) }
Write-Output 'INDEX_STATE'
$indexState | ForEach-Object { Write-Output $_ }
Write-Output 'STAT'
$stat | ForEach-Object { Write-Output $_ }
if ($coreDirty) { Write-Output 'CORE_STATUS'; $coreStatus | ForEach-Object { Write-Output $_ } }
Write-Output "IN_SCOPE_COUNT=$($inScope.Count + $untracked.Count)"
if ($inScope.Count + $untracked.Count -eq 0) { exit 3 }
exit 0
