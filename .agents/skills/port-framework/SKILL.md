---
name: port-framework
description: >-
  列出游戏库 td_trunk 相对核心库在 Assets/HotScripts/Framework 与
  Assets/AOTScripts 下的改动，供人工审核后再回写。在用户要求回写框架、
  审核 td_trunk 框架改动、同步 Framework 或同步 AOTScripts 到核心库时使用。
version: "1.0"
---

# 框架改动回写

把游戏库里的框架改动先列给用户审核，确认后再写入核心库，并合并回游戏库。只读脚本不做提交。

## 何时使用

- 回写框架、审核 td_trunk 框架改动、同步 Framework、同步 AOTScripts
- 用户只说「审核」：列出后停止
- 用户已看过清单并回复「确认回写」：执行写入
- 同一条消息写明「不用再确认，直接回写」：列出之后可以接着执行

未出现上述意图时，不要运行本技能的写入步骤。

## 仓库与范围

- 核心库：`E:/WorkGitHubZn/XianzyUnity`
- 游戏库：`E:/KaboomZnWork/ka0_client/ka02_trunk/main/XDefense/td_trunk`
- 默认范围只有 `Assets/HotScripts/Framework` 与 `Assets/AOTScripts`
- 比较基准是核心库当前 `HEAD`。用户确认时点名的额外路径只加入这一次

## 只读列举

在任何写入之前运行：

```powershell
powershell -NoProfile -File E:\WorkGitHubZn\XianzyUnity\.agents\skills\port-framework\scripts\list-framework-diff.ps1
```

脚本只执行 `rev-parse`、`status`、`diff`、`remote`、`merge-base`、`cat-file`。退出码 `0` 表示范围内有改动，`3` 表示没有，`1` 表示仓库检查失败。脚本若报告核心库 `HEAD` 不在游戏库，先在游戏库执行 `git fetch upstream`（不改工作区），再重跑脚本。

## 审核清单

用简体中文输出，然后停止，等待用户回复：

```markdown
## 框架回写审核

- 核心库：`<HEAD>`，工作区干净 / 不干净；该提交是否为游戏库 HEAD 的祖先
- 游戏库：`<HEAD>`，upstream/master 是否已指向核心库 HEAD
- upstream 推送地址：（须为 DISABLED 才允许之后写入）

### 将回写（N）
- `M` / `A` / `D` / `R` / `??` 路径

### 留下不回写（N）
- 范围外改动，含 ProjectSettings 与产品目录

### 范围内统计
（脚本 STAT 段）

### 拟定提交说明
一句话说明框架缺陷的目的。

确认回写后将：核心库只提交这些路径，游戏库只还原这些路径，fetch，merge upstream/master --no-edit，push backup。不推 GitHub。
回复「确认回写」后执行。要加入范围外路径，请在回复里点名。
```

范围内没有改动时，说明游戏库这两棵目录与核心库 `HEAD` 一致，不要创建空提交。

## 停止，不要写入

出现任一情况就只报告：

- 核心库工作区不干净
- 核心库 `HEAD` 不是游戏库 `HEAD` 的祖先（游戏库尚未并入核心库最新提交）
- 游戏库 `upstream` 的 push 地址不是 `DISABLED`
- 范围内没有改动，且用户没有点名额外路径
- 已确认集合里有二进制文件（脚本 `BINARY_IN_SCOPE`，或补丁含 `GIT binary patch`）
- `git apply --check` 失败
- 未跟踪文件的目标路径在核心库已经存在
- `fetch` 之后 `HEAD..upstream/master` 里除刚提交的那一笔之外还有别的提交，或列表为空

## 确认后的写入

先重新核对 `upstream` 的 push 地址是 `DISABLED`，并再跑一次只读脚本。已确认路径以用户看到的清单为准；用户点名的额外路径并入这一次。

1. 已跟踪差异用补丁。不要用 PowerShell 的 `>` 重定向补丁。

```powershell
git -C <游戏库> diff --output=<临时补丁> <核心库HEAD> -- <已确认的已跟踪路径>
git -C <核心库> apply --check <临时补丁>
git -C <核心库> apply <临时补丁>
```

没有已跟踪差异就跳过补丁。用完删除临时补丁，不要把它提交进仓库。

2. 未跟踪文件按相同相对路径复制到核心库。复制前目标必须不存在。用文件复制，不要用 `Get-Content | Set-Content`。

3. 核心库里 `git diff --name-only` 与 `git status --porcelain -uall` 出现的路径都必须属于已确认路径。然后只暂存这些路径：

```powershell
git add -- <已确认路径>
```

`git diff --cached --name-only` 必须是已确认路径的子集，且不能为空。否则不要提交。

4. 提交说明用审核清单里的那一句；用户在确认时另给了说明则用用户的。提交后核对 `git status`，暂存区里不得残留范围外文件。

5. 回到游戏库，只还原已跟踪的已确认路径：

```powershell
git restore --source=HEAD --staged --worktree -- <已跟踪路径>
```

6. 核心库提交成功后，只删除游戏库里那些已复制走的未跟踪文件。不要 `git clean`，不要删除父目录。

7. 合并并备份：

```powershell
git fetch upstream
git log --oneline HEAD..upstream/master
git merge upstream/master --no-edit
git push backup master
```

`HEAD..upstream/master` 在合并前应只含刚提交的那一笔。合并失败时执行 `git merge --abort`，报告冲突路径，不要把范围外文件放进合并结果。

8. 把上述命令的退出结果、新的提交哈希、`git status -sb` 发回对话。范围外改动保持原样。

## 禁止

- `git add -A`、`git add .`、`git restore .`、`git clean`
- `git push origin`、`git push upstream`、向 GitHub 或核心库推送
- `--no-verify`、`--force`、改写历史
- 修改 git config
- 把游戏库 `master` 合并进核心库
- 把补丁、`ProjectSettings` 或未点名的产品文件提交进核心库
