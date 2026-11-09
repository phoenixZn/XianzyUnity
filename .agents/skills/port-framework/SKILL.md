---
name: port-framework
description: >-
  审核游戏库相对核心库的框架改动，确认后回写核心库并合并回游戏库。
  用于回写框架、审核待回写改动，或将 Framework、AOTScripts 改动同步到核心库的任务。
---

# 框架改动回写

默认范围为 `Assets/HotScripts/Framework` 与 `Assets/AOTScripts`。用户点名的额外路径只加入本次任务。只要求审核时列出后停止；已有针对清单的明确回写授权，或用户要求直接回写时，可以继续，但不跳过检查和备份。

## 仓库与预检

从当前工作区和本次任务已明确指定的 `core_repo`、`game_repo` 解析路径，每次重新核对远程配置，不搜索磁盘或套用其他任务的路径。

- 当前仓库有 `upstream` 且 push 地址为 `DISABLED` 时，视为游戏库，核心库取其 fetch 地址；否则视为核心库，游戏库须由用户明确指定。
- 核心库必须是可访问的本地工作区，游戏库的 `upstream` 必须指向它；本地地址按实际 Git 目录核对，无法确认映射则停止。
- 写入前两库均须在 `master`，且没有进行中的 merge、rebase、cherry-pick 或 revert。核心库须干净，不自动切换分支或清理文件。
- 先在游戏库执行 `git fetch upstream`，失败即停止。核心库 `HEAD` 须为游戏库 `HEAD` 的祖先，且 `upstream/master` 须等于核心库 `HEAD`。

运行随附的只读脚本，额外路径以数组传入；路径是仓库相对的字面路径，不使用通配符：

```powershell
& '<本技能目录>/scripts/list-framework-diff.ps1' -CoreRepo '<核心库>' -GameRepo '<游戏库>'
# 有额外路径时，在同一个 PowerShell 会话中追加参数，例如：
& '<本技能目录>/scripts/list-framework-diff.ps1' -CoreRepo '<核心库>' -GameRepo '<游戏库>' -ExtraPath @('额外目录/文件')
```

脚本不 fetch 或写入，只查询状态、读取文件并计算哈希。退出码 `1` 表示检查失败，`3` 表示没有待回写差异，`0` 表示有差异；`BLOCKED=yes` 时仍不得写入。

## 审核快照

用简体中文展示两库路径、分支及完整 `HEAD`、`UPSTREAM_HEAD`、远程地址、`ROOTS`、`BLOCKERS`、待回写路径和统计、范围外未提交改动，以及拟定提交说明。重命名以删除和新增分别列出，两个路径都属于审核集合。

保存完整脚本输出和 `REVIEW_SHA256`。它绑定仓库、基线、范围、状态、暂存差异及待回写文件字节；不能只凭文件名沿用确认。额外路径也经过相同检查，新增或删减范围后重新生成清单。

以下任一项阻断写入：预检不通过、二进制文件、未跟踪文件在核心库存在同名目标、特殊文件或不安全路径、补丁无法应用。没有差异时不创建空提交。

需要确认时，说明将只提交审核路径、备份后清理游戏库相应改动、合并核心库新提交；存在 `backup` 远程时再备份推送，不推 GitHub。用户确认针对这份快照生效。

## 写入与合并

每条命令检查退出码；失败即进入 [恢复流程](references/recovery.md)，不能继续后续步骤。下文 `<已确认路径>` 指清单中的完整文件集合，包含删除路径，不把整个目录直接当作暂存或清理范围。

1. 再 fetch 并重跑只读脚本，确认无阻断，且 `REVIEW_SHA256` 与已审核值一致。变化则重新审核。
2. 按 [恢复流程中的备份约定](references/recovery.md#执行前备份)创建独立备份，保存游戏库所选路径的原始文件字节、存在状态及暂存补丁，并记录两库基线和范围。备份失败不得修改核心库。备份后再核对快照，防止复制期间源内容变化。
3. 对已跟踪差异生成补丁并在核心库检查、应用。所有路径参数使用 Git 全局选项 `--literal-pathspecs`；不用 PowerShell 的 `>` 重定向补丁：

```powershell
git --literal-pathspecs -C <游戏库> diff --no-ext-diff --no-textconv --no-renames --output=<补丁> <已审核CORE_HEAD> -- <已确认的已跟踪路径>
git -C <核心库> apply --check <补丁>
git -C <核心库> apply <补丁>
```

没有已跟踪差异则跳过补丁。未跟踪文件按原相对路径复制，目标须不存在；先在核心库内创建所需父目录，补丁可能已经删除原来的空目录。创建、复制与删除前核对绝对路径位于指定仓库内，且路径链中不存在重解析点；保留原始字节。

4. 核心库所有工作区与暂存变化必须属于审核集合，暂存区应恰好包含实际回写差异且不能为空。只 `git --literal-pathspecs add -- <已确认路径>`，审阅暂存补丁，按仓库既有换行与过滤规则核对内容对应已审核来源，再使用审核中的说明提交。记录新提交 `PORT_COMMIT`，验证其父提交为已审核 `CORE_HEAD`、改动未越界、提交后核心库干净；验证或钩子失败均停止，不跳过钩子。
5. **清理游戏库前**再 fetch，要求 `upstream/master` 等于 `PORT_COMMIT`，且 `git log --format=%H <已审核GAME_HEAD>..upstream/master` 只包含这一笔。再次确认两库分支、游戏库 `HEAD`、游戏库所选文件字节和暂存状态未变。此阶段失败，游戏库尚未清理，保留核心库提交和备份并报告。
6. 记录清理阶段开始，只还原已确认的已跟踪路径：

```powershell
git --literal-pathspecs -C <游戏库> restore --source=<已审核GAME_HEAD> --staged --worktree -- <已确认的已跟踪路径>
```

只删除清单中已备份的未跟踪文件，不删除父目录。清理前逐个核对当前文件与备份相同；发现新修改立即停止，不覆盖。记录清理后的路径状态和哈希，供失败恢复时确认没有后续编辑。

7. 再核对游戏库仍在审核的 `master` 基线、核心库仍在 `PORT_COMMIT`，且 `upstream/master` 未变，然后执行 `git merge upstream/master --no-edit`。若 Git 因范围外脏文件拒绝合并，不 stash 或清理它们，按恢复流程恢复已清理的路径。
8. 合并成功后核对 `PORT_COMMIT` 已成为游戏库 `HEAD` 的祖先、所选文件符合审核内容、范围外未提交改动与暂存状态保持原样。成功后不执行失败回滚。存在 `backup` 时核对其所有推送目标均为本地备份地址，再执行 `git push backup master`；缺失则跳过，不创建远程。推送失败仅报告备份失败，不撤回已完成合并。

完成后报告两库提交、各阶段结果、`git status -sb` 和备份目录。确认本地合并及备份均成功、原始状态不再需要恢复后，才删除本次临时备份；失败时保留。

## 边界

- 不使用 `git add -A`、`git add .`、`git restore .`、`git clean`、`git stash`。
- 不 push origin/upstream，不向 GitHub 或核心库推送；不使用 `--no-verify`、`--force` 或改写历史。
- 不修改 Git 配置，不把游戏库 `master` 合入核心库，不提交补丁、备份或未审核路径。
