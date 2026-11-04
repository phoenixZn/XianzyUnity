---
name: merge-upstream
description: >-
  列出当前游戏库 master 尚未包含的 upstream/master 提交和文件，经确认后再合并。
  在用户要求合并 upstream、拉取核心库更新、把 upstream/master 合并进 master 或更新框架时使用。
version: "1.0"
---

# 合并 upstream

把当前游戏库的 `upstream/master` 合并进 `master`。先列出提交和文件，用户确认后再合并。只读脚本不做合并。

合并的是 `master` 尚未包含的全部 upstream 提交，不按目录过滤。不推送，不 rebase，不 reset。

## 何时使用

- 合并 upstream、拉取核心库更新、把 upstream/master 合并进 master、更新框架
- 用户只说「列出」或「审核」：列出后停止
- 用户已看过清单并回复「确认合并」：执行合并
- 同一条消息写明「不用再确认，直接合并」：列出之后可以接着执行。这句话不跳过下面的阻断条件

未出现上述意图时，不要运行本技能的合并步骤。

## 仓库

每次从当前仓库读取，不搜索磁盘，也不沿用旧对话里的路径。技能正文不写核心库盘符或游戏工程名。

- 用户这条消息写了仓库路径：该路径是游戏库。
- 否则游戏库是 `git rev-parse --show-toplevel`。
- 游戏库存在远程 `upstream`，且 `git remote get-url --push upstream` 等于 `DISABLED`：当前仓库是游戏库。核心库只取 `git remote get-url upstream` 的文本，用来展示。
- 没有 `upstream`，或推送地址不是 `DISABLED`：停止。不要把当前仓库当成核心库去找一个游戏库，也不要向别的工程合并。

合并只用远程跟踪分支 `upstream/master`。不要打开核心库工作区，不要对核心库路径执行 git。`upstream` 只是远程别名；换机器或换衍生工程时，仍读这个游戏库里配置的地址。

## 只读列举

在游戏库根目录先执行 `git fetch upstream`。fetch 失败就停止。fetch 只更新远程跟踪分支。

然后运行本技能目录里的脚本：

```powershell
powershell -NoProfile -File .agents/skills/merge-upstream/scripts/list-upstream-incoming.ps1 -GameRepo <游戏库>
```

游戏库就是当前仓库根时可以省略 `-GameRepo`。不要改去调用别的工作区里的副本。

脚本只执行 `rev-parse`、`status`、`diff`、`remote`、`merge-base`、`log`。退出码 `0` 表示 `master` 还有尚未包含的 upstream 提交，`3` 表示已经包含，`1` 表示仓库检查失败。

即将进入的提交是 `master..upstream/master`。当前分支就是 `master` 时，这与 `HEAD..upstream/master` 相同。

## 审核清单

退出码 `3`：只说明 `master` 已包含 `upstream/master`，不要合并。

退出码 `1`：转述脚本的 `ERROR` 行，不要合并。

退出码 `0`：用简体中文写清游戏库、核心库、upstream 推送地址、当前分支和是否阻断，并附上脚本原文，然后停止。`ON_MASTER=no` 且 `CHECKOUT_MASTER=ok` 时，写明确认后会先切到 `master` 再合并。`CHECKOUT_MASTER=blocked` 或 `OVERLAP=yes` 时写明这次不会合并，不要写「回复确认合并后执行」。

```markdown
## upstream 合并审核

- 游戏库：`<GAME_REPO>`
- 核心库：`<CORE_URL>`
- upstream 推送地址：`<UPSTREAM_PUSH>`（须为 DISABLED）
- 当前分支 `<BRANCH>`，HEAD `<HEAD>`，master `<MASTER>`
- upstream/master：`<UPSTREAM_HEAD>`
- 切到 master：`<CHECKOUT_MASTER>`；脏文件重叠：`<OVERLAP>`

（粘贴脚本原文）

确认合并后将：必要时先切到 master，再执行 `git merge upstream/master --no-edit`。不推送。
回复「确认合并」后执行。
```

## 停止，不要合并

出现任一情况就只报告：

- 没有 `upstream`，或推送地址不是 `DISABLED`
- `CHECKOUT_MASTER=blocked`（当前不在 `master`，本地改动会挡住切换）
- `OVERLAP=yes`（脏文件与即将进来的路径重叠）
- 已有未完成的 merge 或 rebase
- 退出码是 `1` 或 `3`
- 确认后再次 fetch，`INCOMING_HASHES` 与用户看到的清单不一致

## 确认后的合并

命令都在游戏库根目录执行。再 fetch 一次，并再跑一次只读脚本。以这次输出为准。

`INCOMING_HASHES` 必须与审核清单逐行相同、顺序相同。不同就把新日志发给用户并停止。重跑后退出码不是 `0`，或出现上面的阻断条件，也停止。

1. `ON_MASTER=no` 且 `CHECKOUT_MASTER=ok` 时，在游戏库执行：

```powershell
git checkout master
```

失败则停止，不要合并。

2. 合并：

```powershell
git merge upstream/master --no-edit
```

本地改动导致命令拒绝、且没有进入合并状态时：不要 stash，不要覆盖，报告原话并停止。

已经进入冲突合并时：先列出 `git diff --name-only --diff-filter=U`，再执行 `git merge --abort`。不要手工改冲突文件。

3. 成功后把合并提交哈希和 `git status -sb` 发回对话。清单以外的未提交改动保持原样。

## 禁止

- `git add -A`、`git add .`、`git stash`、`git clean`
- `reset`、`rebase`、`--force`、`--no-verify`、`--allow-unrelated-histories`
- 修改 git config
- 任何 push
- 对核心库工作区执行 git
