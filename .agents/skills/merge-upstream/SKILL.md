---
name: merge-upstream
description: >-
  列出游戏库 master 尚未包含的 upstream/master 提交和文件，审核后合并。
  用于合并 upstream、拉取核心库更新或将核心框架更新合入游戏库的任务。
---

# 合并 upstream

合并 `master..upstream/master` 的全部提交，不按目录过滤，不推送。只要求列出或审核时，输出清单后停止；已有针对该清单的明确合并授权，或用户要求直接合并时，可以继续，但仍须通过以下检查。

## 仓库与列举

游戏库使用本次任务明确指定的路径，否则取当前仓库根目录。每次重新读取该仓库的远程配置，不搜索磁盘或套用其他任务的路径。

- 必须存在 `upstream`，且 `git remote get-url --push upstream` 为 `DISABLED`，否则停止。
- 核心库只展示 `git remote get-url upstream` 的文本；不打开其工作区，也不对其执行 Git。
- 先在游戏库执行 `git fetch upstream`，失败即停止。然后运行本技能随附的只读脚本：

```powershell
& '<本技能目录>/scripts/list-upstream-incoming.ps1' -GameRepo '<游戏库>'
```

脚本只查询 Git 状态，不 fetch 或合并。退出码 `1`：报告 `ERROR` 并停止；`3`：说明已经包含 upstream，无需合并；`0`：按输出整理审核清单。

## 审核与阻断

用简体中文展示：

- `GAME_REPO`、`CORE_URL`、`UPSTREAM_PUSH`。
- `BRANCH`、`HEAD`、`MASTER`、`UPSTREAM_HEAD`，均保留完整提交哈希作为审核基线。
- `CHECKOUT_MASTER`、`OVERLAP`，以及相关阻断路径。
- `INCOMING`、`NAME_STATUS`、`STAT` 和范围外未提交改动；保留完整 `INCOMING_HASHES` 供执行前核对。较长的原始输出可随审核文档保存，无须在聊天重复粘贴。

`CHECKOUT_MASTER=blocked`、`OVERLAP=yes`、已有 merge/rebase 或脚本失败时，不进入合并步骤。允许切换分支时，说明将先切到 `master`。需要确认时，只对无阻断的清单提出确认请求。

## 执行

1. 在同一游戏库再 fetch，并重跑脚本。仓库、远程地址、`BRANCH`、`HEAD`、`MASTER`、`UPSTREAM_HEAD` 和完整 `INCOMING_HASHES` 必须与已审核快照一致；同时重新检查阻断条件。变化时展示新清单，重新审核，不沿用旧确认。
2. `ON_MASTER=no` 且 `CHECKOUT_MASTER=ok` 时执行 `git checkout master`，失败即停止。随后确认当前分支为 `master`、`HEAD` 等于已审核 `MASTER`。
3. 再确认 `upstream/master` 仍等于已审核 `UPSTREAM_HEAD`，执行 `git merge upstream/master --no-edit`。
4. 合并拒绝且未进入 merge 状态时，报告原因并停止。进入冲突状态时，先列出 `git diff --name-only --diff-filter=U`，再执行 `git merge --abort` 并检查退出码；abort 失败则报告当前状态，停止继续操作。
5. 成功后展示实际 `HEAD` 和 `git status -sb`。快进合并也按成功处理，不声称一定生成了合并提交。

## 边界

- 保留未提交改动；不自动 stash、覆盖或手工解决冲突。
- 不使用 `git add -A`、`git add .`、`git clean`、`reset`、`rebase`、`--force`、`--no-verify` 或 `--allow-unrelated-histories`。
- 不修改 Git 配置，不执行任何 push，不操作核心库工作区。
