# git 别名清单

这些别名写在全局设置 `C:\Users\chang\.gitconfig` 的 `[alias]` 一节里，对这台机器上所有仓库都有效，在 PowerShell、Git Bash、Rider 的终端里用法一样。用的时候前面都要带 `git`，例如 `git st`。

## 看：现在是什么状态、改了什么

| 别名 | 等于 | 用来做什么 |
|---|---|---|
| `git st` | `git status -sb` | 一行一个文件地列出改动，第一行是当前分支 |
| `git df` | `git diff` | 看还没暂存的改动 |
| `git dfs` | `git diff --staged` | 看已经暂存、下一次提交会带上的改动 |
| `git lg` | `git log --graph …` | 一行一条的提交历史：短哈希、日期、说明、分支、作者 |
| `git lga` | `git lg --all` | 同上，但包含所有分支 |
| `git last` | `git log -1 HEAD --stat` | 最近一次提交改了哪些文件 |

`git df`、`git dfs` 后面可以跟文件名只看那一个文件，也可以跟 `--stat` 只看每个文件改了几行。`git lg -10` 只看最近 10 条。

## 存：暂存与提交

| 别名 | 等于 | 用来做什么 |
|---|---|---|
| `git aa` | `git add -A` | 把所有改动（含新文件、删除）加进暂存区 |
| `git ap` | `git add -p` | 一块一块地挑着暂存，每块问一次要不要 |
| `git ci` | `git commit` | 提交，会打开编辑器写说明 |
| `git cm "说明"` | `git commit -m "说明"` | 提交，说明直接写在命令里 |
| `git amend` | `git commit --amend --no-edit` | 把暂存的改动并进上一次提交，说明不变 |

## 撤：后悔了怎么办

| 别名 | 等于 | 用来做什么 | 会不会丢东西 |
|---|---|---|---|
| `git unstage 文件` | `git restore --staged -- 文件` | 把文件从暂存区拿出来，改动还在 | 不会 |
| `git undo` | `git reset --soft HEAD~1` | 撤掉最近一次提交，它的改动回到暂存区 | 不会 |
| `git discard 文件` | `git restore -- 文件` | 丢掉这个文件还没暂存的改动，回到上次提交的样子 | **会，找不回来** |

`git unstage .` 和 `git discard .` 是对当前目录下所有文件做。`git discard` 管不到新建的、从没提交过的文件，那种直接删文件就行。`git amend` 和 `git undo` 只用在还没推送出去的提交上。

## 分支

| 别名 | 等于 | 用来做什么 |
|---|---|---|
| `git br` | `git branch` | 列出本地分支 |
| `git sw 分支` | `git switch 分支` | 切到已有的分支 |
| `git swc 分支` | `git switch -c 分支` | 新建一个分支并切过去 |
| `git co 目标` | `git checkout 目标` | 老写法，切分支或检出某个提交 |

## 同步

| 别名 | 等于 | 用来做什么 |
|---|---|---|
| `git pl` | `git pull` | 拉取并合并远端 |
| `git ps` | `git push` | 推送 |
| `git pf` | `git push --force-with-lease` | 强推，但远端有别人的新提交时会拒绝，不会盖掉 |

## 暂时收起来

| 别名 | 等于 | 用来做什么 |
|---|---|---|
| `git ss` | `git stash push` | 把手头的改动收起来，工作区回到干净 |
| `git sp` | `git stash pop` | 把最近收起来的改动放回来 |
| `git sl` | `git stash list` | 列出收起来的每一份 |

## 审我改的东西，常用的一串

```
git st              有哪些文件变了
git df              逐行看改了什么
git aa              认可的话全部暂存
git dfs --stat      确认要提交的是哪些文件
git cm "说明"       提交
```

不认可某个文件的改动：`git discard 文件`。

## 管理别名

| 要做的事 | 命令 |
|---|---|
| 列出所有别名 | `git aliases` |
| 加一个或改一个 | `git config --global alias.名字 "命令"` |
| 删一个 | `git config --global --unset alias.名字` |
| 全部还原 | 用备份 `C:\Users\chang\.gitconfig.bak-2026-11-15` 覆盖 `.gitconfig` |
